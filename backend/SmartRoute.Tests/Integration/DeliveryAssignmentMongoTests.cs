using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
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
/// US2 sur un vrai MongoDB (base smartroute_test, voir <see cref="TestMongo"/>) :
/// la confirmation écrit réellement l'arrêt dans le document de la tournée.
/// </summary>
[Collection(TestMongo.CollectionName)]
public sealed class DeliveryAssignmentMongoTests(SmartRouteApiFactory factory) : IClassFixture<SmartRouteApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ProposeThenConfirm_WritesTheStopInMongoDb_AndSecondConfirmationIsRejected()
    {
        await TestMongo.EnsureAvailableAsync(Ct);

        // Base de test uniquement : collection vidée pour que la proposition ne voie que cette tournée.
        var routes = new MongoClient(TestMongo.ConnectionString)
            .GetDatabase(TestMongo.DatabaseName)
            .GetCollection<RouteModel>(RouteRepository.CollectionName);
        await routes.DeleteManyAsync(FilterDefinition<RouteModel>.Empty, Ct);

        var route = TestRoutes.Eligible(factory.DriverId);
        await routes.InsertOneAsync(route, cancellationToken: Ct);

        // API branchée sur le vrai RouteRepository (MongoDB) ; comptes et géocodage restent simulés.
        var client = factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(TestMongo.ApiSettings));
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<RouteRepository>();
                    services.AddScoped<RouteRepository>();
                });
            })
            .CreateClient();
        await TestAuth.AuthenticateAsync(client, SmartRouteApiFactory.ManagerEmail, Ct);

        var proposal = await client.PostAsJsonAsync(
            "/api/deliveries/assign-best-driver",
            new { address = new { label = TestRoutes.NewDeliveryLabel } },
            Ct);
        Assert.Equal(HttpStatusCode.OK, proposal.StatusCode);
        var proposed = await proposal.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(route.Id, proposed.GetProperty("routeId").GetString());

        var confirmRequest = new
        {
            address = new { label = TestRoutes.NewDeliveryLabel },
            routeId = route.Id,
            insertionPosition = proposed.GetProperty("insertionPosition").GetInt32(),
            routeVersion = proposed.GetProperty("routeVersion").GetInt32(),
        };

        var confirmation = await client.PostAsJsonAsync("/api/deliveries/confirm-assignment", confirmRequest, Ct);
        Assert.Equal(HttpStatusCode.Created, confirmation.StatusCode);
        var stopId = (await confirmation.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("stopId").GetString();

        // Lecture directe dans MongoDB : l'arrêt est enregistré, l'ordre renuméroté, la version incrémentée.
        var stored = await routes.Find(r => r.Id == route.Id).SingleAsync(Ct);
        Assert.Equal(route.Version + 1, stored.Version);
        Assert.Equal(
            ["Arret A", TestRoutes.NewDeliveryLabel, "Arret C", "Arret B"],
            stored.Stops.OrderBy(s => s.Order).Select(s => s.Address.Label));
        Assert.Equal([1, 2, 3, 4], stored.Stops.OrderBy(s => s.Order).Select(s => s.Order));
        Assert.Contains(stored.Stops, s => s.Id == stopId && s.Status == "PENDING");

        // Même proposition rejouée : le filtre { _id, version } ne trouve plus la tournée → 409, rien n'est écrit.
        var second = await client.PostAsJsonAsync("/api/deliveries/confirm-assignment", confirmRequest, Ct);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var afterSecond = await routes.Find(r => r.Id == route.Id).SingleAsync(Ct);
        Assert.Equal(stored.Version, afterSecond.Version);
        Assert.Equal(4, afterSecond.Stops.Count);
    }
}
