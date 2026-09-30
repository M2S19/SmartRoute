namespace SmartRoute.Api.Authorization;

/// <summary>
/// Politiques par rôle, à utiliser sur les futurs endpoints :
/// <c>[Authorize(Policy = Policies.RequireManager)]</c>.
/// Sans attribut, un endpoint exige seulement un utilisateur authentifié (politique par défaut).
/// </summary>
public static class Policies
{
    public const string RequireManager = nameof(RequireManager);
    public const string RequireDriver = nameof(RequireDriver);
}
