namespace SmartRoute.Api.DTOs.Health;

/// <param name="Status">« ok » si l'API répond.</param>
/// <param name="Mongo">« ok » si MongoDB répond au ping.</param>
public sealed record MongoHealthResponse(string Status, string Mongo);
