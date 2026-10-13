namespace SmartRoute.Api.DTOs.Routes;

public class OptimizeRouteRequest
{
    public required RouteAddressRequest Depot { get; set; }

    public List<RouteAddressRequest> Stops { get; set; } = new();
}

public class RouteAddressRequest
{
    public required string Label { get; set; }
}