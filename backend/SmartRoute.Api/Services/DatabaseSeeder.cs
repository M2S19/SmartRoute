using Microsoft.Extensions.Options;
using SmartRoute.Api.Auth;
using SmartRoute.Api.Models;
using SmartRoute.Api.Repositories;
using SmartRoute.Api.Settings;

namespace SmartRoute.Api.Services;

/// <summary>
/// Crée les comptes de démonstration s'ils n'existent pas (idempotent, jamais d'écrasement).
/// Appelé uniquement en environnement Development (voir Program.cs).
/// </summary>
public sealed class DatabaseSeeder(
    UserRepository users,
    PasswordHasher passwordHasher,
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
            new() { Email = "responsable@smartroute.fr", FullName = "Claire Martin", Role = Roles.ResponsableLogistique, PasswordHash = "" },
            new() { Email = "livreur@smartroute.fr", FullName = "Pierre Dubois", Role = Roles.Livreur, DriverCode = "4092", PasswordHash = "" },
        ];

        foreach (var user in demoUsers)
        {
            if (await users.FindByEmailAsync(user.Email, cancellationToken) is not null) continue;

            user.PasswordHash = passwordHasher.Hash(user, password);
            await users.AddAsync(user, cancellationToken);
            logger.LogInformation("Compte de démonstration créé : {Email} ({Role}).", user.Email, user.Role);
        }
    }
}
