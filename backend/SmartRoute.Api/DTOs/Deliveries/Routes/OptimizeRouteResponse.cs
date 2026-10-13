namespace SmartRoute.Api.DTOs.Routes;

public class OptimizeRouteResponse
{
    public double DistanceBeforeKm { get; set; }

    public double DistanceAfterKm { get; set; }

    public double GainPercent { get; set; }

    public List<OptimizedPointResponse> OptimizedOrder { get; set; } = new();
}

public class OptimizedPointResponse
{
    public int Position { get; set; }

    public required string Label { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}