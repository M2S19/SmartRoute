namespace SmartRoute.Api.Settings;

public sealed class SeedSettings
{
    public const string SectionName = "Seed";

    /// <summary>Mot de passe des comptes de démonstration (donnée de test, Development uniquement).</summary>
    public string DefaultPassword { get; set; } = string.Empty;
}
