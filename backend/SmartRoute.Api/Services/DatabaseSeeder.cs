using System.Globalization;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using SmartRoute.Api.Auth;
using SmartRoute.Api.Models;
using SmartRoute.Api.Repositories;
using SmartRoute.Api.Settings;
using RouteModel = SmartRoute.Api.Models.Route;

namespace SmartRoute.Api.Services;

/// <summary>
/// Crée les données de démonstration si elles n'existent pas (idempotent, jamais d'écrasement) :
/// les deux comptes, puis une tournée du jour affectée au livreur (pour tester l'affectation, US2).
/// Appelé uniquement en environnement Development (voir Program.cs).
/// </summary>
public sealed class DatabaseSeeder(
    UserRepository users,
    RouteRepository routes,
    PasswordHasher passwordHasher,
    TimeProvider timeProvider,
    IOptions<SeedSettings> options,
    ILogger<DatabaseSeeder> logger)
{
    private const string DemoDriverEmail = "livreur@smartroute.fr";

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
            new() { Email = DemoDriverEmail, FullName = "Pierre Dubois", Role = Roles.Livreur, DriverCode = "4092", PasswordHash = "" },
        ];

        foreach (var user in demoUsers)
        {
            if (await users.FindByEmailAsync(user.Email, cancellationToken) is not null) continue;

            user.PasswordHash = passwordHasher.Hash(user, password);
            await users.AddAsync(user, cancellationToken);
            logger.LogInformation("Compte de démonstration créé : {Email} ({Role}).", user.Email, user.Role);
        }

        await SeedTodayRouteAsync(cancellationToken);
    }

    /// <summary>
    /// Tournée du jour (date locale, comme assign-best-driver) si aucune tournée n'existe pour aujourd'hui.
    /// Coordonnées obtenues par le géocodage réel de ces adresses (Géoplateforme) : aucun appel externe ici.
    /// </summary>
    private async Task SeedTodayRouteAsync(CancellationToken cancellationToken)
    {
        string today = timeProvider.GetLocalNow().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (await routes.ExistsForDateAsync(today, cancellationToken)) return;

        User? driver = await users.FindByEmailAsync(DemoDriverEmail, cancellationToken);
        if (driver is null) return;

        var route = new RouteModel
        {
            Name = "Tournée Mulhouse centre",
            Date = today,
            Status = "ASSIGNED",
            Version = 0,
            AssignedDriverId = driver.Id,
            Depot = new GeoPoint { Label = "10 rue de l'Industrie, Mulhouse", Latitude = 47.748007, Longitude = 7.329721 },
            Stops =
            [
                Stop(1, "5 avenue Kennedy, Mulhouse", 47.747936, 7.332845),
                Stop(2, "8 rue de Strasbourg, Mulhouse", 47.754485, 7.334465),
                Stop(3, "12 rue de Bâle, Mulhouse", 47.749767, 7.352017),
            ],
        };

        await routes.InsertAsync(route, cancellationToken);
        logger.LogInformation("Tournée de démonstration créée : {Name} du {Date} ({Driver}).", route.Name, today, driver.FullName);
    }

    private static RouteStop Stop(int order, string label, double latitude, double longitude) => new()
    {
        Id = ObjectId.GenerateNewId().ToString(),
        Order = order,
        Status = "PENDING",
        Address = new GeoPoint { Label = label, Latitude = latitude, Longitude = longitude },
    };
}
