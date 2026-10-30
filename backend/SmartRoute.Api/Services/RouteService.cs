
using SmartRoute.Api.DTOs.Routes;
using SmartRoute.Api.Errors;
using SmartRoute.Api.Models;
using SmartRoute.Api.Repositories;
using RouteModel = SmartRoute.Api.Models.Route;

namespace SmartRoute.Api.Services;

/// <summary>
/// US3 : consultation des tournées et validation des livraisons.
/// Le livreur est identifié exclusivement depuis le JWT.
/// </summary>
public class RouteService
{
    private const int MaxDeliverAttempts = 3;

    private readonly RouteRepository _routeRepository;
    private readonly TimeProvider _timeProvider;

    public RouteService(
        RouteRepository routeRepository,
        TimeProvider timeProvider)
    {
        _routeRepository = routeRepository;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Récupère les tournées affectées au livreur.
    /// </summary>
    public async Task<List<DriverRouteResponse>> GetDriverRoutesAsync(
        string driverId,
        CancellationToken cancellationToken = default)
    {
        List<RouteModel> routes =
            await _routeRepository.FindByDriverAsync(
                driverId,
                cancellationToken);

        return routes.Select(DriverRouteResponse.From).ToList();
    }

    /// <summary>
    /// Valide un arrêt en respectant :
    /// - L'appartenance au livreur
    /// - Le statut de la tournée
    /// - L'ordre des livraisons
    /// - L'idempotence via operationId
    /// - Le verrouillage optimiste MongoDB
    /// </summary>
    public async Task<DriverRouteResponse> DeliverStopAsync(
        string driverId,
        string routeId,
        string stopId,
        CancellationToken cancellationToken = default,
        string? operationId = null)
    {
        // 1. Vérifier le format de l'identifiant d'opération.
        if (operationId is not null &&
            !Guid.TryParseExact(operationId, "D", out _))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                ErrorCodes.Conflict,
                "Idempotency-Key doit contenir un UUID valide.");
        }

        // 2. Réessayer en cas de modification concurrente.
        for (int attempt = 1; attempt <= MaxDeliverAttempts; attempt++)
        {
            // Relire l'état courant dans MongoDB.
            RouteModel route =
                await _routeRepository.FindByIdAsync(
                    routeId,
                    cancellationToken)
                ?? throw RouteNotFound();

            // Vérifier l'appartenance au livreur.
            if (route.AssignedDriverId != driverId)
            {
                throw RouteNotFound();
            }

            // 3. Reconnaître une opération déjà exécutée.
            if (operationId is not null)
            {
                RouteStop? previousOperation = route.Stops
                    .FirstOrDefault(s =>
                        s.DeliveryOperationId == operationId);

                if (previousOperation is not null)
                {
                    // Un UUID ne peut pas valider un autre arrêt
                    // de cette même tournée.
                    if (previousOperation.Id != stopId)
                    {
                        throw new ApiException(
                            StatusCodes.Status409Conflict,
                            ErrorCodes.Conflict,
                            "Cet identifiant d'opération est déjà utilisé pour un autre arrêt.");
                    }

                    // Même opération déjà confirmée : retourner 200,
                    // sans modifier MongoDB ni incrémenter la version.
                    if (previousOperation.Status == "DELIVERED")
                    {
                        return DriverRouteResponse.From(route);
                    }
                }
            }

            // 4. Récupérer l'arrêt demandé.
            RouteStop stop = route.Stops.FirstOrDefault(
                s => s.Id == stopId
            ) ?? throw new ApiException(
                StatusCodes.Status404NotFound,
                ErrorCodes.NotFound,
                "Arrêt introuvable dans cette tournée.");

            // 5. Vérifier les règles métier de l'US3.
            EnsureCanBeDelivered(route, stop);

            // 6. Préparer le nouvel état de la tournée.
            int expectedVersion = route.Version;

            stop.Status = "DELIVERED";
            stop.DeliveredAt = UtcNowToMillisecond();

            // Conserver l'UUID dans le même document MongoDB.
            stop.DeliveryOperationId = operationId;

            route.Status = route.Stops.All(
                s => s.Status == "DELIVERED"
            )
                ? "COMPLETED"
                : "IN_PROGRESS";

            route.Version = expectedVersion + 1;

            // 7. Écriture atomique sous condition de version.
            bool updated =
                await _routeRepository.ReplaceIfVersionMatchesAsync(
                    route,
                    expectedVersion,
                    cancellationToken);

            if (updated)
            {
                return DriverRouteResponse.From(route);
            }

            // Si la version a changé entre la lecture et l'écriture,
            // la boucle recommence sur la version la plus récente.
        }

        // 8. Échec après plusieurs tentatives concurrentes.
        throw new ApiException(
            StatusCodes.Status409Conflict,
            ErrorCodes.Conflict,
            "La tournée a été modifiée pendant la validation. Actualisez puis réessayez.");
    }

    /// <summary>
    /// Vérifie les règles métier de validation des arrêts.
    /// </summary>
    private static void EnsureCanBeDelivered(
        RouteModel route,
        RouteStop stop)
    {
        // La tournée doit être modifiable.
        if (!RouteModel.StatusesAcceptingDeliveries.Contains(route.Status))
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                ErrorCodes.RouteNotModifiable,
                "Cette tournée n'est plus modifiable.");
        }

        // L'arrêt ne doit pas déjà être livré.
        if (stop.Status == "DELIVERED")
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                ErrorCodes.StopAlreadyDelivered,
                "Cet arrêt est déjà livré.");
        }

        // Le livreur doit respecter l'ordre de passage.
        RouteStop? nextStop = route.Stops
            .OrderBy(s => s.Order)
            .FirstOrDefault(s => s.Status != "DELIVERED");

        if (nextStop?.Id != stop.Id)
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                ErrorCodes.StopNotNext,
                "Les arrêts doivent être livrés dans l'ordre de la tournée.");
        }
    }

    /// <summary>
    /// Date UTC arrondie à la milliseconde
    /// pour correspondre à la précision MongoDB.
    /// </summary>
    private DateTime UtcNowToMillisecond()
    {
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;

        return now.AddTicks(
            -(now.Ticks % TimeSpan.TicksPerMillisecond));
    }

    private static ApiException RouteNotFound() =>
        new(
            StatusCodes.Status404NotFound,
            ErrorCodes.NotFound,
            "Tournée introuvable.");
}
