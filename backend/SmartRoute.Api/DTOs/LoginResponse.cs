using SmartRoute.Api.Models;

namespace SmartRoute.Api.DTOs;

/// <param name="Token">JWT à envoyer dans « Authorization: Bearer ».</param>
/// <param name="ExpiresAt">Expiration du token (UTC).</param>
/// <param name="User">Utilisateur connecté.</param>
public sealed record LoginResponse(string Token, DateTime ExpiresAt, UserDto User);

/// <param name="Id">ObjectId MongoDB.</param>
/// <param name="Email">Email normalisé en minuscules.</param>
/// <param name="FullName">Nom affiché.</param>
/// <param name="Role">« RESPONSABLE_LOGISTIQUE » ou « LIVREUR ».</param>
/// <param name="DriverCode">Présent uniquement pour un livreur.</param>
public sealed record UserDto(string Id, string Email, string FullName, string Role, string? DriverCode)
{
    public static UserDto From(User user) => new(user.Id, user.Email, user.FullName, user.Role, user.DriverCode);
}
