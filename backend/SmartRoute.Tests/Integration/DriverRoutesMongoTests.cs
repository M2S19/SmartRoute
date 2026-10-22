using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using SmartRoute.Api.Repositories;
using SmartRoute.Tests.Common;
using RouteModel = SmartRoute.Api.Models.Route;

namespace SmartRoute.Tests.Integration;

/// <summary>
/// US3 sur un vrai MongoDB (base smartroute_test, voir <see cref="TestMongo"/>) :
/// l'état final du document est relu directement dans la base, pas seulement le code HTTP.
/// Même collection que <see cref="DeliveryAssignmentMongoTests"/> : exécution séquentielle (collection xUnit « MongoDB »).
/// </summary>
[Collection(TestMongo.CollectionName)]
public sealed class DriverRoutesMongoTests(SmartRouteApiFactory factory) : IClassFixture<SmartRouteApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string DeliverUrl(string routeId, string stopId) => $"/api/routes/me/{routeId}/stops/{stopId}/deliver";

    private static HttpRequestMessage Patch(string url) => new(HttpMethod.Patch, url);

    /// <summary>Collection « routes » vidée (base de test uniquement), puis ces tournées insérées.</summary>
    private static async Task<IMongoCollection<RouteModel>> ResetRoutesAsync(params RouteModel[] seed)
    {
        await TestMongo.EnsureAvailableAsync(Ct);

        var routes = new MongoClient(TestMongo.ConnectionString)
            .GetDatabase(TestMongo.DatabaseName)
            .GetCollection<RouteModel>(RouteRepository.CollectionName);
        await routes.DeleteManyAsync(FilterDefinition<RouteModel>.Empty, Ct);
        await routes.InsertManyAsync(seed, cancellationToken: Ct);
        return routes;
    }

    /// <summary>API branchée sur le vrai RouteRepository (MongoDB) ; comptes et géocodage restent simulés.</summary>
    private WebApplicationFactory<Program> MongoApi() => factory.WithWebHostBuilder(builder =>
    {
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(TestMongo.ApiSettings));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<RouteRepository>();
            services.AddScoped<RouteRepository>();
        });
    });

    private static async Task<HttpClient> ClientAsync(WebApplicationFactory<Program> api, string email)
    {
        var client = api.CreateClient();
        await TestAuth.AuthenticateAsync(client, email, Ct);
        return client;
    }

    private static Task<RouteModel> StoredAsync(IMongoCollection<RouteModel> routes, string id) =>
        routes.Find(r => r.Id == id).SingleAsync(Ct);

    [Fact]
    public async Task GetMine_ThenDeliver_WritesTheStatusInMongoDb_AndOtherDriverRouteIsUntouched()
    {
        var mine = TestRoutes.Eligible(factory.DriverId);
        var other = TestRoutes.Eligible(factory.OtherDriverId);
        var routes = await ResetRoutesAsync(mine, other);
        var driver = await ClientAsync(MongoApi(), SmartRouteApiFactory.DriverEmail);

        // Consultation : seule la tournée du livreur connecté est renvoyée.
        var list = await (await driver.GetAsync("/api/routes/me", Ct)).Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal([mine.Id], list.EnumerateArray().Select(r => r.GetProperty("id").GetString()));
        var firstStopId = list[0].GetProperty("stops")[0].GetProperty("id").GetString()!;

        var response = await driver.SendAsync(Patch(DeliverUrl(mine.Id, firstStopId)), Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Lecture directe dans MongoDB.
        var stored = await StoredAsync(routes, mine.Id);
        Assert.Equal(mine.Version + 1, stored.Version);
        Assert.Equal("IN_PROGRESS", stored.Status);
        Assert.Equal(["DELIVERED", "PENDING", "PENDING"], stored.Stops.OrderBy(s => s.Order).Select(s => s.Status));
        var deliveredAt = stored.Stops.Single(s => s.Id == firstStopId).DeliveredAt;
        Assert.NotNull(deliveredAt);
        Assert.Equal(DateTimeKind.Utc, deliveredAt.Value.Kind);
        // La réponse renvoie exactement la date enregistrée.
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(deliveredAt.Value, body.GetProperty("stops")[0].GetProperty("deliveredAt").GetDateTime().ToUniversalTime());

        // Après « actualisation », l'état relu par l'API est celui de la base.
        var reloaded = await (await driver.GetAsync("/api/routes/me", Ct)).Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("DELIVERED", reloaded[0].GetProperty("stops")[0].GetProperty("status").GetString());

        // Arrêt déjà livré : 409, la base ne change pas.
        var again = await driver.SendAsync(Patch(DeliverUrl(mine.Id, firstStopId)), Ct);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(stored.Version, (await StoredAsync(routes, mine.Id)).Version);

        // Tournée d'un autre livreur : 404, aucune écriture.
        var forbidden = await driver.SendAsync(Patch(DeliverUrl(other.Id, other.Stops[0].Id)), Ct);
        Assert.Equal(HttpStatusCode.NotFound, forbidden.StatusCode);
        var otherStored = await StoredAsync(routes, other.Id);
        Assert.Equal(other.Version, otherStored.Version);
        Assert.All(otherStored.Stops, s => Assert.Equal("PENDING", s.Status));
    }

    [Fact]
    public async Task Deliver_AllStops_CompletesTheRouteInMongoDb()
    {
        var route = TestRoutes.Eligible(factory.DriverId);
        var routes = await ResetRoutesAsync(route);
        var driver = await ClientAsync(MongoApi(), SmartRouteApiFactory.DriverEmail);

        foreach (var stop in route.Stops.OrderBy(s => s.Order))
        {
            Assert.Equal(HttpStatusCode.OK, (await driver.SendAsync(Patch(DeliverUrl(route.Id, stop.Id)), Ct)).StatusCode);
        }

        var stored = await StoredAsync(routes, route.Id);
        Assert.Equal("COMPLETED", stored.Status);
        Assert.Equal(route.Version + 3, stored.Version);
        Assert.All(stored.Stops, s => Assert.Equal("DELIVERED", s.Status));
    }

    [Fact]
    public async Task Deliver_ConcurrentRequestsOnSameStop_WritesOnce()
    {
        var route = TestRoutes.Eligible(factory.DriverId);
        var routes = await ResetRoutesAsync(route);
        var driver = await ClientAsync(MongoApi(), SmartRouteApiFactory.DriverEmail);
        var stopId = route.Stops.Single(s => s.Order == 1).Id;

        var responses = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => driver.SendAsync(Patch(DeliverUrl(route.Id, stopId)), Ct)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.OK),
            r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        var stored = await StoredAsync(routes, route.Id);
        Assert.Equal(route.Version + 1, stored.Version);
        Assert.Equal(["DELIVERED", "PENDING", "PENDING"], stored.Stops.OrderBy(s => s.Order).Select(s => s.Status));
    }

    [Fact]
    public async Task Deliver_ConcurrentWithUs2Confirmation_NoChangeIsLost()
    {
        var route = TestRoutes.Eligible(factory.DriverId);
        var routes = await ResetRoutesAsync(route);
        var api = MongoApi();
        var driver = await ClientAsync(api, SmartRouteApiFactory.DriverEmail);
        var manager = await ClientAsync(api, SmartRouteApiFactory.ManagerEmail);

        // Proposition US2 sur la version initiale, puis confirmation et livraison lancées en même temps.
        var proposal = await (await manager.PostAsJsonAsync(
                "/api/deliveries/assign-best-driver", new { address = new { label = TestRoutes.NewDeliveryLabel } }, Ct))
            .Content.ReadFromJsonAsync<JsonElement>(Ct);
        var confirmTask = manager.PostAsJsonAsync("/api/deliveries/confirm-assignment", new
        {
            address = new { label = TestRoutes.NewDeliveryLabel },
            routeId = route.Id,
            insertionPosition = proposal.GetProperty("insertionPosition").GetInt32(),
            routeVersion = proposal.GetProperty("routeVersion").GetInt32(),
        }, Ct);
        var deliverTask = driver.SendAsync(Patch(DeliverUrl(route.Id, route.Stops.Single(s => s.Order == 1).Id)), Ct);
        await Task.WhenAll(confirmTask, deliverTask);

        var stored = await StoredAsync(routes, route.Id);
        var ordered = stored.Stops.OrderBy(s => s.Order).ToList();

        // La livraison est toujours enregistrée : elle est réappliquée si la confirmation est passée avant elle.
        Assert.Equal(HttpStatusCode.OK, deliverTask.Result.StatusCode);
        Assert.Equal("DELIVERED", ordered.Single(s => s.Address.Label == "Arret A").Status);

        if (confirmTask.Result.StatusCode == HttpStatusCode.Created)
        {
            // Confirmation écrite d'abord : les deux modifications sont présentes.
            Assert.Equal(route.Version + 2, stored.Version);
            Assert.Equal(["Arret A", TestRoutes.NewDeliveryLabel, "Arret C", "Arret B"], ordered.Select(s => s.Address.Label));
            Assert.Equal("PENDING", ordered[1].Status);
        }
        else
        {
            // Livraison écrite d'abord : la proposition est périmée, rien d'autre n'est écrit.
            Assert.Equal(HttpStatusCode.Conflict, confirmTask.Result.StatusCode);
            Assert.Equal(route.Version + 1, stored.Version);
            Assert.Equal(3, stored.Stops.Count);
        }
        Assert.Equal(Enumerable.Range(1, stored.Stops.Count), ordered.Select(s => s.Order));
    }
}
