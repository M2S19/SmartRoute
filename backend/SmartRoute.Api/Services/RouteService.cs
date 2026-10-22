using SmartRoute.Api.DTOs.Routes;
using SmartRoute.Api.Errors;
using SmartRoute.Api.Models;
using SmartRoute.Api.Repositories;
using RouteModel = SmartRoute.Api.Models.Route;

namespace SmartRoute.Api.Services;

/// <summary>
/// Tournées du livreur connecté (US3) : consultation et validation d'un arrêt.
/// L'identifiant du livreur vient toujours du JWT (claim « sub »), jamais du client.
/// </summary>
public class RouteService
{
    /// <summary>Lectures / écritures tentées si la tournée change entre la lecture et l'écriture.</summary>
    private const int MaxDeliverAttempts = 3;

    private readonly RouteRepository _routeRepository;
    private readonly TimeProvider _timeProvider;

    public RouteService(RouteRepository routeRepository, TimeProvider timeProvider)
    {
        _routeRepository = routeRepository;
        _timeProvider = timeProvider;
    }

    /// <summary>Tournées affectées au livreur (liste vide s'il n'en a aucune).</summary>
    public async Task<List<DriverRouteResponse>> GetDriverRoutesAsync(
        string driverId,
        CancellationToken cancellationToken = default)
    {
        List<RouteModel> routes = await _routeRepository.FindByDriverAsync(driverId, cancellationToken);

        return routes.Select(DriverRouteResponse.From).ToList();
    }

    /// <summary>
    /// Marque un arrêt comme livré et renvoie la tournée à jour.
    /// Règles : tournée du livreur, encore modifiable (ASSIGNED ou IN_PROGRESS), arrêt en attente,
    /// et arrêt suivant dans l'ordre de passage (les arrêts livrés forment toujours le début de la tournée,
    /// ce que suppose aussi l'insertion de l'US2).
    /// Transitions : arrêt PENDING → DELIVERED ; tournée ASSIGNED → IN_PROGRESS au premier arrêt livré,
    /// → COMPLETED quand tous les arrêts sont livrés.
    /// </summary>
    /// <exception cref="ApiException">
    /// 404 NOT_FOUND (tournée inconnue ou d'un autre livreur, arrêt absent de la tournée),
    /// 409 ROUTE_NOT_MODIFIABLE, STOP_ALREADY_DELIVERED, STOP_NOT_NEXT,
    /// 409 CONFLICT (tournée modifiée en continu pendant la validation).
    /// </exception>
    public async Task<DriverRouteResponse> DeliverStopAsync(
        string driverId,
        string routeId,
        string stopId,
        CancellationToken cancellationToken = default)
    {
        for (int attempt = 1; attempt <= MaxDeliverAttempts; attempt++)
        {
            // 1. Relecture à chaque tentative : les règles sont revérifiées sur l'état courant.
            RouteModel route = await _routeRepository.FindByIdAsync(routeId, cancellationToken) ?? throw RouteNotFound();

            // Même réponse que pour une tournée inexistante : rien n'est révélé des tournées des autres livreurs.
            if (route.AssignedDriverId != driverId)
            {
                throw RouteNotFound();
            }

            RouteStop stop = route.Stops.FirstOrDefault(s => s.Id == stopId)
                ?? throw new ApiException(StatusCodes.Status404NotFound, ErrorCodes.NotFound, "Arrêt introuvable dans cette tournée.");

            EnsureCanBeDelivered(route, stop);

            // 2. Nouvel état de la tournée.
            int expectedVersion = route.Version;
            stop.Status = "DELIVERED";
            stop.DeliveredAt = UtcNowToMillisecond();
            route.Status = route.Stops.All(s => s.Status == "DELIVERED") ? "COMPLETED" : "IN_PROGRESS";
            route.Version = expectedVersion + 1;

            // 3. Écriture conditionnelle { _id, version } : si une autre écriture (ex. confirmation US2)
            //    est passée entre-temps, rien n'est écrit et on recommence sur la nouvelle version.
            if (await _routeRepository.ReplaceIfVersionMatchesAsync(route, expectedVersion, cancellationToken))
            {
                return DriverRouteResponse.From(route);
            }
        }

        throw new ApiException(
            StatusCodes.Status409Conflict,
            ErrorCodes.Conflict,
            "La tournée a été modifiée pendant la validation. Actualisez puis réessayez.");
    }

    private static void EnsureCanBeDelivered(RouteModel route, RouteStop stop)
    {
        if (!RouteModel.StatusesAcceptingDeliveries.Contains(route.Status))
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                ErrorCodes.RouteNotModifiable,
                "Cette tournée n'est plus modifiable.");
        }

        if (stop.Status == "DELIVERED")
        {
            throw new ApiException(
                StatusCodes.Status409Conflict,
                ErrorCodes.StopAlreadyDelivered,
                "Cet arrêt est déjà livré.");
        }

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

    // MongoDB conserve les dates à la milliseconde : la réponse renvoie exactement la valeur enregistrée.
    private DateTime UtcNowToMillisecond()
    {
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMillisecond));
    }

    private static ApiException RouteNotFound() =>
        new(StatusCodes.Status404NotFound, ErrorCodes.NotFound, "Tournée introuvable.");
}
