namespace SmartRoute.Core.Enums;

public enum UserRole
{
    ResponsableLogistique,
    Livreur,
}

/// <summary>
/// Valeurs textuelles des rôles, identiques partout : JSON, claim JWT « role », MongoDB, front.
/// </summary>
public static class UserRoleNames
{
    public const string ResponsableLogistique = "RESPONSABLE_LOGISTIQUE";
    public const string Livreur = "LIVREUR";

    public static string ToName(this UserRole role) => role switch
    {
        UserRole.ResponsableLogistique => ResponsableLogistique,
        UserRole.Livreur => Livreur,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Rôle inconnu."),
    };

    public static bool TryParse(string? name, out UserRole role)
    {
        switch (name)
        {
            case ResponsableLogistique:
                role = UserRole.ResponsableLogistique;
                return true;
            case Livreur:
                role = UserRole.Livreur;
                return true;
            default:
                role = default;
                return false;
        }
    }
}
