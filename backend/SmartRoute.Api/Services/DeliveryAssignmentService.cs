using MongoDB.Bson;
using SmartRoute.Api.Algorithms;
using SmartRoute.Api.DTOs.Deliveries;
using SmartRoute.Api.Errors;
using SmartRoute.Api.Models;
using SmartRoute.Api.Repositories;
using RouteModel = SmartRoute.Api.Models.Route;

namespace SmartRoute.Api.Services;

public class DeliveryAssignmentService
{
    private readonly GeocodingService _geocodingService;
    private readonly DistanceService _distanceService;
    private readonly CheapestInsertion _cheapestInsertion;
    private readonly RouteRepository _routeRepository;
    private readonly UserRepository _userRepository;

    public DeliveryAssignmentService(
        GeocodingService geocodingService,
        DistanceService distanceService,
        CheapestInsertion cheapestInsertion,
        RouteRepository routeRepository,
        UserRepository userRepository)
    {
        _geocodingService = geocodingService;
        _distanceService = distanceService;
        _cheapestInsertion = cheapestInsertion;
        _routeRepository = routeRepository;
        _userRepository = userRepository;

    }

    /// <summary>
    /// Proposition (rien n'est enregistré) : tournée du jour au plus faible surcoût et position d'insertion.
    /// </summary>
    /// <exception cref="ApiException">422 NO_ELIGIBLE_ROUTE si aucune tournée du jour ne peut recevoir la livraison.</exception>
    public async Task<AssignmentProposalResponse> FindBestRouteAsync(
        string address,
        string date,
        CancellationToken cancellationToken = default)
    {
        // 1. Géocoder la nouvelle livraison
        GeoPoint newPoint = await GeocodeDeliveryAsync(address);

        // 2. Récupérer les tournées éligibles dans MongoDB
        List<RouteModel> routes =
            await _routeRepository.FindEligibleRoutesAsync(
                date,
                cancellationToken
            );

        RouteModel? bestRoute = null;
        int bestInsertionPosition = -1;
        double bestExtraDistance = double.MaxValue;

        // 3. Tester chaque tournée
        foreach (RouteModel route in routes)
        {
            var (insertionPosition, extraDistance) = EvaluateInsertion(route, newPoint);

            // Comparer avec la meilleure tournée trouvée
            if (extraDistance < bestExtraDistance)
            {
                bestRoute = route;
                bestInsertionPosition = insertionPosition;
                bestExtraDistance = extraDistance;
            }
        }

        if (bestRoute is null)
        {
            throw new ApiException(
                StatusCodes.Status422UnprocessableEntity,
                ErrorCodes.NoEligibleRoute,
                "Aucune tournée active ne peut recevoir cette livraison aujourd'hui.");
        }

        User driver = await FindDriverAsync(bestRoute, cancellationToken);

        return new AssignmentProposalResponse
        {
            RouteId = bestRoute.Id!,
            RouteName = bestRoute.Name,
            RouteVersion = bestRoute.Version,

            Driver = ToDriverSummary(driver),

            InsertionPosition = bestInsertionPosition,
            ExtraDistanceKm = bestExtraDistance
        };
    }

    /// <summary>
    /// Confirmation : revérifie la proposition puis insère la livraison dans la tournée (MongoDB).
    /// Le client n'est pas cru sur parole : tournée, éligibilité, version et position sont recontrôlées.
    /// </summary>
    /// <exception cref="ApiException">
    /// 404 NOT_FOUND (tournée inconnue), 409 ROUTE_NOT_ELIGIBLE (tournée qui n'accepte plus de livraison),
    /// 409 PROPOSAL_OUTDATED (tournée modifiée depuis la proposition, ou confirmation déjà enregistrée).
    /// </exception>
    public async Task<ConfirmAssignmentResponse> ConfirmAssignmentAsync(
        ConfirmAssignmentRequest request,
        string date,
        CancellationToken cancellationToken = default)
    {
        // 1. La tournée existe et accepte encore des livraisons aujourd'hui.
        RouteModel route = await _routeRepository.FindByIdAsync(request.RouteId, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, ErrorCodes.NotFound, "Tournée introuvable.");

        if (!AcceptsDeliveries(route, date))
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                ErrorCodes.RouteNotEligible,
                "Cette tournée ne peut plus recevoir de nouvelle livraison.");
        }

        // 2. La tournée n'a pas changé depuis la proposition (sinon la position n'est plus valable).
        if (route.Version != request.RouteVersion)
        {
            throw ProposalOutdated();
        }

        // 3. Recalcul serveur de la position : même calcul que la proposition.
        GeoPoint newPoint = await GeocodeDeliveryAsync(request.Address.Label);
        var (insertionPosition, extraDistance) = EvaluateInsertion(route, newPoint);

        if (insertionPosition != request.InsertionPosition)
        {
            throw ProposalOutdated();
        }

        User driver = await FindDriverAsync(route, cancellationToken);

        // 4. Insertion du nouvel arrêt et renumérotation de l'ordre de passage (1..n).
        List<RouteStop> orderedStops = route.Stops.OrderBy(stop => stop.Order).ToList();
        var newStop = new RouteStop
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Order = insertionPosition,
            Status = "PENDING",
            Address = newPoint,
        };
        orderedStops.Insert(insertionPosition - 1, newStop);

        for (int i = 0; i < orderedStops.Count; i++)
        {
            orderedStops[i].Order = i + 1;
        }

        // 5. Écriture conditionnelle : seulement si la version en base est toujours celle lue.
        //    Une double confirmation ou une écriture concurrente ne trouve plus cette version → 409.
        int expectedVersion = route.Version;
        route.Stops = orderedStops;
        route.Version = expectedVersion + 1;

        bool saved = await _routeRepository.ReplaceIfVersionMatchesAsync(route, expectedVersion, cancellationToken);
        if (!saved)
        {
            throw ProposalOutdated();
        }

        return new ConfirmAssignmentResponse
        {
            RouteId = route.Id,
            RouteName = route.Name,
            RouteVersion = route.Version,
            StopId = newStop.Id,
            InsertionPosition = insertionPosition,
            ExtraDistanceKm = extraDistance,
            Driver = ToDriverSummary(driver),
        };
    }

    /// <summary>
    /// Meilleure position d'insertion d'un point dans une tournée (Cheapest Insertion),
    /// sans revenir avant le dernier arrêt déjà livré.
    /// </summary>
    /// <returns>Position métier dans la tournée complète (1 = premier arrêt) et surcoût en km.</returns>
    private (int InsertionPosition, double ExtraDistance) EvaluateInsertion(RouteModel route, GeoPoint newPoint)
    {
        var orderedStops =
            route.Stops
                .OrderBy(stop => stop.Order)
                .ToList();

        // Liste utilisée pour construire la matrice :
        // dépôt + tous les stops + nouvelle livraison
        List<GeoPoint> points = new List<GeoPoint>();

        points.Add(route.Depot);

        foreach (RouteStop stop in orderedStops)
        {
            points.Add(stop.Address);
        }

        points.Add(newPoint);

        int newPointIndex = points.Count - 1;

        double[,] matrix =
            _distanceService.BuildDistanceMatrix(points);

        // Chercher le dernier arrêt déjà livré.
        int lastDeliveredIndex =
            orderedStops.FindLastIndex(
                stop => stop.Status == "DELIVERED"
            );

        // Construire uniquement le chemin encore à parcourir.
        List<int> remainingRoute = new List<int>();

        if (lastDeliveredIndex == -1)
        {
            // Aucun arrêt livré :
            // on démarre depuis le dépôt.
            remainingRoute.Add(0);
        }
        else
        {
            // +1 car matrix[0] = dépôt
            remainingRoute.Add(lastDeliveredIndex + 1);
        }

        // Ajouter les stops restant après le dernier livré.
        for (
            int i = lastDeliveredIndex + 1;
            i < orderedStops.Count;
            i++)
        {
            remainingRoute.Add(i + 1);
        }

        // Retour au dépôt.
        remainingRoute.Add(0);

        // Cheapest Insertion pour cette tournée
        var result =
            _cheapestInsertion.FindBestInsertion(
                newPointIndex,
                matrix,
                remainingRoute
            );

        // Position métier dans la tournée complète.
        int insertionPosition =
            (lastDeliveredIndex + 1)
            + result.Position;

        return (insertionPosition, result.ExtraDistance);
    }

    private async Task<GeoPoint> GeocodeDeliveryAsync(string address)
    {
        var coordinates =
            await _geocodingService.GeocodeAsync(address);

        return new GeoPoint
        {
            Label = address,
            Latitude = coordinates.Latitude,
            Longitude = coordinates.Longitude
        };
    }

    private async Task<User> FindDriverAsync(RouteModel route, CancellationToken cancellationToken)
    {
        User? driver = await _userRepository.FindByIdAsync(
            route.AssignedDriverId!,
            cancellationToken
        );

        if (driver is null)
        {
            throw new Exception("Livreur introuvable.");
        }

        return driver;
    }

    /// <summary>Mêmes règles que <see cref="RouteRepository.FindEligibleRoutesAsync"/>.</summary>
    private static bool AcceptsDeliveries(RouteModel route, string date) =>
        route.Date == date
        && RouteModel.StatusesAcceptingDeliveries.Contains(route.Status)
        && !string.IsNullOrEmpty(route.AssignedDriverId);

    private static DriverSummaryResponse ToDriverSummary(User driver) => new()
    {
        Id = driver.Id!,
        FullName = driver.FullName,
        DriverCode = driver.DriverCode
    };

    private static ApiException ProposalOutdated() => new(
        StatusCodes.Status409Conflict,
        ErrorCodes.ProposalOutdated,
        "La tournée a changé depuis la proposition. Relancez la recherche du meilleur livreur.");
}
