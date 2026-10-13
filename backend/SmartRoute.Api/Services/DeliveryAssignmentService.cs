using SmartRoute.Api.Algorithms;
using SmartRoute.Api.DTOs.Deliveries;
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

    public async Task<AssignmentProposalResponse> FindBestRouteAsync(
        string address,
        string date,
        CancellationToken cancellationToken = default)
    {
        // 1. Géocoder la nouvelle livraison
        var coordinates =
            await _geocodingService.GeocodeAsync(address);

        GeoPoint newPoint = new GeoPoint
        {
            Label = address,
            Latitude = coordinates.Latitude,
            Longitude = coordinates.Longitude
        };

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

            // 4. Cheapest Insertion pour cette tournée
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

            // 5. Comparer avec la meilleure tournée trouvée
            if (result.ExtraDistance < bestExtraDistance)
            {
                bestRoute = route;
                bestInsertionPosition = insertionPosition;
                bestExtraDistance = result.ExtraDistance;
            }
        }

        if (bestRoute is null)
        {
            throw new Exception(
                "Aucune tournée active ne peut recevoir cette livraison.");
        }
        User? driver = await _userRepository.FindByIdAsync(
        bestRoute.AssignedDriverId!,
        cancellationToken
        );

        if (driver is null)
        {
            throw new Exception("Livreur introuvable.");
        }

        return new AssignmentProposalResponse
        {
            RouteId = bestRoute.Id!,
            RouteName = bestRoute.Name,
            RouteVersion = bestRoute.Version,

            Driver = new DriverSummaryResponse
            {
                Id = driver.Id!,
                FullName = driver.FullName,
                DriverCode = driver.DriverCode
            },

            InsertionPosition = bestInsertionPosition,
            ExtraDistanceKm = bestExtraDistance
        };
    }
}