using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRoute.Core.Entities;
using SmartRoute.Core.Enums;
using SmartRoute.Core.Interfaces;

namespace SmartRoute.Infrastructure.Seeding;

public sealed class SeedSettings
{
    public const string SectionName = "Seed";

    /// <summary>Mot de passe de démonstration des comptes créés (donnée de test).</summary>
    public string DefaultPassword { get; set; } = string.Empty;
}

/// <summary>
/// Crée les comptes de démonstration s'ils n'existent pas. Idempotent, jamais d'écrasement.
/// Appelé uniquement en environnement Development (voir Program.cs).
/// </summary>
public sealed class DatabaseSeeder(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IOptions<SeedSettings> options,
    ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var password = options.Value.DefaultPassword;
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Seed ignoré : « Seed:DefaultPassword » n'est pas configuré.");
            return;
        }

        User[] demoUsers =
        [
            new()
            {
                Email = "responsable@smartroute.fr",
                FullName = "Claire Martin",
                Role = UserRole.ResponsableLogistique,
                PasswordHash = string.Empty,
            },
            new()
            {
                Email = "livreur@smartroute.fr",
                FullName = "Pierre Dubois",
                Role = UserRole.Livreur,
                DriverCode = "4092",
                PasswordHash = string.Empty,
            },
        ];

        foreach (var user in demoUsers)
        {
            if (await users.FindByEmailAsync(user.Email, cancellationToken) is not null) continue;

            user.PasswordHash = passwordHasher.Hash(password);
            await users.AddAsync(user, cancellationToken);
            logger.LogInformation("Compte de démonstration créé : {Email} ({Role}).", user.Email, user.Role.ToName());
        }
    }
}
