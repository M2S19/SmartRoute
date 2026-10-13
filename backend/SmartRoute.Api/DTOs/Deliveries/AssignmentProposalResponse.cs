namespace SmartRoute.Api.DTOs.Deliveries;

public class AssignmentProposalResponse
{
    public required string RouteId { get; set; }

    public required string RouteName { get; set; }

    public int RouteVersion { get; set; }

    public required DriverSummaryResponse Driver { get; set; }

    public int InsertionPosition { get; set; }

    public double ExtraDistanceKm { get; set; }
}