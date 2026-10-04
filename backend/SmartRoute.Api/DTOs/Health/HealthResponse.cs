namespace SmartRoute.Api.DTOs.Health;

/// <param name="Status">« ok » si l'API répond.</param>
public sealed record HealthResponse(string Status);
