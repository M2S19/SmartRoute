using SmartRoute.Core.Enums;

namespace SmartRoute.Core.Entities;

public class User
{
    /// <summary>ObjectId MongoDB en hexadécimal ; vide tant que l'utilisateur n'est pas enregistré.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Toujours normalisé (voir <see cref="NormalizeEmail"/>).</summary>
    public required string Email { get; set; }

    public required string FullName { get; set; }

    public required string PasswordHash { get; set; }

    public required UserRole Role { get; set; }

    /// <summary>Identifiant affiché d'un livreur, ex. « 4092 ».</summary>
    public string? DriverCode { get; set; }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
