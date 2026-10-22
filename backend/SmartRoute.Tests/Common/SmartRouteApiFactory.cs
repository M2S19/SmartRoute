using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartRoute.Api.Auth;
using SmartRoute.Api.Models;
using SmartRoute.Api.Repositories;
using SmartRoute.Api.Services;

namespace SmartRoute.Tests.Common;

/// <summary>
/// API hébergée en mémoire : repository en mémoire, clé JWT de test, pas d'initialisation MongoDB.
/// Ne dépend ni de Docker ni des user-secrets (exécutable en CI).
/// </summary>
public sealed class SmartRouteApiFactory : WebApplicationFactory<Program>
{
    public const string ManagerEmail = "responsable@smartroute.fr";
    public const string DriverEmail = "livreur@smartroute.fr";
    public const string OtherDriverEmail = "livreur2@smartroute.fr";
    public const string Password = "Test-Password-123";

    // 48 octets : au-dessus du minimum de 32 vérifié au démarrage. Clé de test, pas un secret.
    private const string TestSigningKey = "smartroute-tests-signing-key-not-a-real-secret!!";

    /// <summary>Comptes de test (en mémoire), propres à cette instance de l'API.</summary>
    public InMemoryUserRepository Users { get; } = CreateUsers();

    /// <summary>Tournées (en mémoire), propres à cette instance de l'API ; vides au départ.</summary>
    public InMemoryRouteRepository Routes { get; } = new();

    /// <summary>Identifiant du compte livreur de test.</summary>
    public string DriverId => Users.FindByEmailAsync(DriverEmail).GetAwaiter().GetResult()!.Id;

    /// <summary>Identifiant d'un second livreur (cloisonnement des tournées, US3).</summary>
    public string OtherDriverId => Users.FindByEmailAsync(OtherDriverEmail).GetAwaiter().GetResult()!.Id;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Ni Development (pas de user-secrets, pas de seed) ni Production.
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = TestSigningKey,
            ["Database:InitializeOnStartup"] = "false",
        }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<UserRepository>();
            services.AddSingleton<UserRepository>(Users);
            services.RemoveAll<RouteRepository>();
            services.AddSingleton<RouteRepository>(Routes);

            // Géocodage simulé : aucun appel à Internet pendant les tests.
            services.AddHttpClient<GeocodingService>()
                .ConfigurePrimaryHttpMessageHandler(() => new FakeGeocodingHandler());
        });
    }

    private static InMemoryUserRepository CreateUsers()
    {
        var hasher = new PasswordHasher();
        var users = new InMemoryUserRepository();

        User[] accounts =
        [
            new() { Email = ManagerEmail, FullName = "Claire Martin", Role = Roles.ResponsableLogistique, PasswordHash = "" },
            new() { Email = DriverEmail, FullName = "Pierre Dubois", Role = Roles.Livreur, DriverCode = "4092", PasswordHash = "" },
            new() { Email = OtherDriverEmail, FullName = "Sophie Leroy", Role = Roles.Livreur, DriverCode = "5120", PasswordHash = "" },
        ];

        foreach (var account in accounts)
        {
            account.PasswordHash = hasher.Hash(account, Password);
            users.AddAsync(account).GetAwaiter().GetResult();
        }

        return users;
    }
}
