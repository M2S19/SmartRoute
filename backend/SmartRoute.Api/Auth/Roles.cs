namespace SmartRoute.Api.Auth;

/// <summary>Valeurs des rôles, identiques dans MongoDB, le claim JWT « role », le JSON et le front.</summary>
public static class Roles
{
    public const string ResponsableLogistique = "RESPONSABLE_LOGISTIQUE";
    public const string Livreur = "LIVREUR";
}

/// <summary>
/// Politiques d'autorisation (déclarées dans Program.cs), pour les futurs endpoints :
/// <c>[Authorize(Policy = Policies.RequireManager)]</c>.
/// Sans attribut, un endpoint exige seulement un utilisateur authentifié.
/// </summary>
public static class Policies
{
    public const string RequireManager = nameof(RequireManager);
    public const string RequireDriver = nameof(RequireDriver);
}

/// <summary>Noms de claims courts, partagés entre l'émission et la validation du token.</summary>
public static class ClaimNames
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Role = "role";
}
