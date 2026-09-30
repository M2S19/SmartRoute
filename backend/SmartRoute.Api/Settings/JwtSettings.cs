using System.Text;

namespace SmartRoute.Api.Settings;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    public const int MinimumKeyBytes = 32;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int LifetimeHours { get; set; } = 12;

    /// <summary>Clé HS256. Uniquement via user-secrets (ou variable d'environnement), jamais dans le dépôt.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public byte[] SigningKeyBytes => Encoding.UTF8.GetBytes(SigningKey);
}
