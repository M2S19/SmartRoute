namespace SmartRoute.Api.DTOs.Deliveries;

public class AssignBestDriverRequest
{
    public required AddressRequest Address { get; set; }

    public string ParcelType { get; set; } = "STANDARD";

    public string? Instructions { get; set; }
}

public class AddressRequest
{
    public required string Label { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}