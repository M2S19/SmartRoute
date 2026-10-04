using SmartRoute.Api.Models;

namespace SmartRoute.Api.DTOs.Auth;

/// <param name="Id">ObjectId MongoDB.</param>
/// <param name="Email">Email normalisé en minuscules.</param>
/// <param name="FullName">Nom affiché.</param>
/// <param name="Role">« RESPONSABLE_LOGISTIQUE » ou « LIVREUR ».</param>
/// <param name="DriverCode">Présent uniquement pour un livreur.</param>
public sealed record UserDto(string Id, string Email, string FullName, string Role, string? DriverCode)
{
    public static UserDto From(User user) => new(user.Id, user.Email, user.FullName, user.Role, user.DriverCode);
}
