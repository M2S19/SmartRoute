namespace SmartRoute.Api.DTOs.Deliveries;

public class DriverSummaryResponse
{
    public required string Id { get; set; }

    public required string FullName { get; set; }

    public string? DriverCode { get; set; }
}