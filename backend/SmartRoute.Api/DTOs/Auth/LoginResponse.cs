namespace SmartRoute.Api.DTOs.Auth;

/// <param name="Token">JWT à envoyer dans « Authorization: Bearer ».</param>
/// <param name="ExpiresAt">Expiration du token (UTC).</param>
/// <param name="User">Utilisateur connecté.</param>
public sealed record LoginResponse(string Token, DateTime ExpiresAt, UserDto User);
