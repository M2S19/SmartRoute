using SmartRoute.Api.Models;
using RouteModel = SmartRoute.Api.Models.Route;

namespace SmartRoute.Api.DTOs.Routes;

/// <summary>
/// Tournée vue par le livreur (US3) : GET /api/routes/me et PATCH …/deliver.
/// Ni la version (concurrence, gérée par le serveur) ni l'identifiant du livreur ne sont exposés.
/// </summary>
public class DriverRouteResponse
{
    public required string Id { get; set; }

    public required string Name { get; set; }

    /// <summary>Jour de la tournée (yyyy-MM-dd).</summary>
    public required string Date { get; set; }

    /// <summary>ASSIGNED, IN_PROGRESS ou COMPLETED.</summary>
    public required string Status { get; set; }

    public required RoutePointResponse Depot { get; set; }

    /// <summary>Arrêts dans l'ordre de passage (order croissant, à partir de 1).</summary>
    public List<DriverStopResponse> Stops { get; set; } = new();

    public static DriverRouteResponse From(RouteModel route) => new()
    {
        Id = route.Id,
        Name = route.Name,
        Date = route.Date,
        Status = route.Status,
        Depot = RoutePointResponse.From(route.Depot),
        Stops = route.Stops.OrderBy(stop => stop.Order).Select(DriverStopResponse.From).ToList(),
    };
}

public class DriverStopResponse
{
    public required string Id { get; set; }

    public int Order { get; set; }

    /// <summary>PENDING ou DELIVERED.</summary>
    public required string Status { get; set; }

    public required RoutePointResponse Address { get; set; }

    /// <summary>Date UTC de la livraison ; absente tant que l'arrêt n'est pas livré.</summary>
    public DateTime? DeliveredAt { get; set; }

    public static DriverStopResponse From(RouteStop stop) => new()
    {
        Id = stop.Id,
        Order = stop.Order,
        Status = stop.Status,
        Address = RoutePointResponse.From(stop.Address),
        DeliveredAt = stop.DeliveredAt,
    };
}

public class RoutePointResponse
{
    public required string Label { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public static RoutePointResponse From(GeoPoint point) => new()
    {
        Label = point.Label,
        Latitude = point.Latitude,
        Longitude = point.Longitude,
    };
}
