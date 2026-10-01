namespace SmartRoute.Api.DTOs;

/// <param name="Status">« ok » si l'API et ses dépendances répondent.</param>
/// <param name="Mongo">« ok » si MongoDB répond au ping.</param>
public sealed record HealthResponse(string Status, string Mongo);
