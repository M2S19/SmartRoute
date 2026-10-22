using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MongoDB.Bson;
using SmartRoute.Tests.Common;
using RouteModel = SmartRoute.Api.Models.Route;

namespace SmartRoute.Tests.Routes;

/// <summary>
/// US3 — GET /api/routes/me et PATCH /api/routes/me/{routeId}/stops/{stopId}/deliver,
/// sur tournées en mémoire (<see cref="InMemoryRouteRepository"/>).
/// </summary>
public sealed class DriverRoutesTests(SmartRouteApiFactory factory) : IClassFixture<SmartRouteApiFactory>
{
    private const string MyRoutesUrl = "/api/routes/me";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string DeliverUrl(string routeId, string stopId) => $"/api/routes/me/{routeId}/stops/{stopId}/deliver";

    private async Task<HttpClient> ClientAsync(string email)
    {
        var client = factory.CreateClient();
        await TestAuth.AuthenticateAsync(client, email, Ct);
        return client;
    }

    private Task<HttpClient> DriverClientAsync() => ClientAsync(SmartRouteApiFactory.DriverEmail);

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Ct);

    private static async Task<JsonElement> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var problem = await JsonAsync(response);
        Assert.Equal(code, problem.GetProperty("code").GetString());
        return problem;
    }

    private static string StopId(RouteModel route, int order) => route.Stops.Single(s => s.Order == order).Id;

    /// <summary>Marque les <paramref name="count"/> premiers arrêts comme livrés.</summary>
    private static RouteModel WithDelivered(RouteModel route, int count, string status = "IN_PROGRESS")
    {
        foreach (var stop in route.Stops.OrderBy(s => s.Order).Take(count))
        {
            stop.Status = "DELIVERED";
            stop.DeliveredAt = DateTime.UtcNow.AddMinutes(-10);
        }
        route.Status = status;
        return route;
    }

    private static HttpRequestMessage Patch(string url) => new(HttpMethod.Patch, url);

    // ---------- Consultation ----------

    [Fact]
    public async Task GetMine_AsDriver_ReturnsOnlyOwnRoutesWithOrderedStops()
    {
        var mine = TestRoutes.Eligible(factory.DriverId);
        mine.Stops.Reverse(); // ordre de stockage différent de l'ordre de passage
        var older = TestRoutes.Eligible(factory.DriverId, date: "2000-01-01", status: "COMPLETED");
        var other = TestRoutes.Eligible(factory.OtherDriverId);
        factory.Routes.Reset(mine, older, other);

        var response = await (await DriverClientAsync()).GetAsync(MyRoutesUrl, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var routes = (await JsonAsync(response)).EnumerateArray().ToList();
        Assert.Equal([mine.Id, older.Id], routes.Select(r => r.GetProperty("id").GetString()));

        var first = routes[0];
        // Contrat : ni version, ni identifiant du livreur.
        Assert.Equal(["date", "depot", "id", "name", "status", "stops"], first.EnumerateObject().Select(p => p.Name).Order());
        Assert.Equal("ASSIGNED", first.GetProperty("status").GetString());
        Assert.Equal("Depot Sud", first.GetProperty("depot").GetProperty("label").GetString());

        var stops = first.GetProperty("stops").EnumerateArray().ToList();
        Assert.Equal([1, 2, 3], stops.Select(s => s.GetProperty("order").GetInt32()));
        Assert.Equal(["Arret A", "Arret C", "Arret B"], stops.Select(s => s.GetProperty("address").GetProperty("label").GetString()));
        Assert.All(stops, s => Assert.Equal("PENDING", s.GetProperty("status").GetString()));
        Assert.All(stops, s => Assert.False(s.TryGetProperty("deliveredAt", out _)));
    }

    [Fact]
    public async Task GetMine_OtherDriver_NeverSeesFirstDriverRoutes()
    {
        var mine = TestRoutes.Eligible(factory.DriverId);
        var other = TestRoutes.Eligible(factory.OtherDriverId);
        factory.Routes.Reset(mine, other);

        var response = await (await ClientAsync(SmartRouteApiFactory.OtherDriverEmail)).GetAsync(MyRoutesUrl, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([other.Id], (await JsonAsync(response)).EnumerateArray().Select(r => r.GetProperty("id").GetString()));
    }

    [Fact]
    public async Task GetMine_NoRoute_ReturnsEmptyList()
    {
        factory.Routes.Reset(TestRoutes.Eligible(factory.OtherDriverId));

        var response = await (await DriverClientAsync()).GetAsync(MyRoutesUrl, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await JsonAsync(response);
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Empty(body.EnumerateArray());
    }

    [Fact]
    public async Task GetMine_AsManager_Returns403()
    {
        var response = await (await ClientAsync(SmartRouteApiFactory.ManagerEmail)).GetAsync(MyRoutesUrl, Ct);

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "FORBIDDEN");
    }

    [Fact]
    public async Task GetMine_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().GetAsync(MyRoutesUrl, Ct);

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    // ---------- Livraison : cas nominal ----------

    [Fact]
    public async Task Deliver_NextStop_SavesStatusAndReturnsUpdatedRoute()
    {
        var route = TestRoutes.Eligible(factory.DriverId);
        factory.Routes.Reset(route);
        var before = DateTime.UtcNow.AddSeconds(-1);

        var response = await (await DriverClientAsync()).SendAsync(Patch(DeliverUrl(route.Id, StopId(route, 1))), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await JsonAsync(response);
        Assert.Equal(route.Id, body.GetProperty("id").GetString());
        Assert.Equal("IN_PROGRESS", body.GetProperty("status").GetString());
        var delivered = body.GetProperty("stops")[0];
        Assert.Equal("DELIVERED", delivered.GetProperty("status").GetString());
        Assert.True(delivered.GetProperty("deliveredAt").GetDateTime() >= before);

        // Enregistré : arrêt livré, tournée en cours, version incrémentée, autres arrêts inchangés.
        var stored = factory.Routes.Get(route.Id)!;
        Assert.Equal(4, stored.Version);
        Assert.Equal("IN_PROGRESS", stored.Status);
        Assert.Equal(["DELIVERED", "PENDING", "PENDING"], stored.Stops.OrderBy(s => s.Order).Select(s => s.Status));
        Assert.NotNull(stored.Stops.Single(s => s.Order == 1).DeliveredAt);
    }

    [Fact]
    public async Task Deliver_LastStop_CompletesRoute()
    {
        var route = WithDelivered(TestRoutes.Eligible(factory.DriverId), count: 2);
        factory.Routes.Reset(route);

        var response = await (await DriverClientAsync()).SendAsync(Patch(DeliverUrl(route.Id, StopId(route, 3))), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("COMPLETED", (await JsonAsync(response)).GetProperty("status").GetString());
        var stored = factory.Routes.Get(route.Id)!;
        Assert.Equal("COMPLETED", stored.Status);
        Assert.All(stored.Stops, s => Assert.Equal("DELIVERED", s.Status));
    }

    // ---------- Livraison : refus ----------

    [Fact]
    public async Task Deliver_UnknownStopOrRoute_Returns404()
    {
        var route = TestRoutes.Eligible(factory.DriverId);
        factory.Routes.Reset(route);
        var client = await DriverClientAsync();

        var unknownStop = await client.SendAsync(Patch(DeliverUrl(route.Id, ObjectId.GenerateNewId().ToString())), Ct);
        var unknownRoute = await client.SendAsync(Patch(DeliverUrl(ObjectId.GenerateNewId().ToString(), StopId(route, 1))), Ct);
        var malformedRoute = await client.SendAsync(Patch(DeliverUrl("pas-un-objectid", StopId(route, 1))), Ct);

        await AssertProblemAsync(unknownStop, HttpStatusCode.NotFound, "NOT_FOUND");
        await AssertProblemAsync(unknownRoute, HttpStatusCode.NotFound, "NOT_FOUND");
        await AssertProblemAsync(malformedRoute, HttpStatusCode.NotFound, "NOT_FOUND");
        Assert.Equal(3, factory.Routes.Get(route.Id)!.Version);
    }

    [Fact]
    public async Task Deliver_RouteOfAnotherDriver_Returns404AndSavesNothing()
    {
        var other = TestRoutes.Eligible(factory.OtherDriverId);
        factory.Routes.Reset(other);

        var response = await (await DriverClientAsync()).SendAsync(Patch(DeliverUrl(other.Id, StopId(other, 1))), Ct);

        var problem = await AssertProblemAsync(response, HttpStatusCode.NotFound, "NOT_FOUND");
        // Même message qu'une tournée inexistante : rien n'est révélé.
        Assert.Equal("Tournée introuvable.", problem.GetProperty("detail").GetString());
        var stored = factory.Routes.Get(other.Id)!;
        Assert.Equal(3, stored.Version);
        Assert.All(stored.Stops, s => Assert.Equal("PENDING", s.Status));
    }

    [Fact]
    public async Task Deliver_AlreadyDeliveredStop_Returns409AndSavesNothing()
    {
        var route = WithDelivered(TestRoutes.Eligible(factory.DriverId), count: 1);
        var deliveredAt = route.Stops.Single(s => s.Order == 1).DeliveredAt;
        factory.Routes.Reset(route);

        var response = await (await DriverClientAsync()).SendAsync(Patch(DeliverUrl(route.Id, StopId(route, 1))), Ct);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "STOP_ALREADY_DELIVERED");
        var stored = factory.Routes.Get(route.Id)!;
        Assert.Equal(3, stored.Version);
        Assert.Equal(deliveredAt!.Value, stored.Stops.Single(s => s.Order == 1).DeliveredAt!.Value, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Deliver_CompletedRoute_Returns409()
    {
        // Tournée terminée contenant encore un arrêt en attente : aucune modification possible.
        var route = WithDelivered(TestRoutes.Eligible(factory.DriverId), count: 2, status: "COMPLETED");
        factory.Routes.Reset(route);

        var response = await (await DriverClientAsync()).SendAsync(Patch(DeliverUrl(route.Id, StopId(route, 3))), Ct);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "ROUTE_NOT_MODIFIABLE");
        Assert.Equal("PENDING", factory.Routes.Get(route.Id)!.Stops.Single(s => s.Order == 3).Status);
    }

    [Fact]
    public async Task Deliver_StopOutOfOrder_Returns409()
    {
        var route = TestRoutes.Eligible(factory.DriverId);
        factory.Routes.Reset(route);

        var response = await (await DriverClientAsync()).SendAsync(Patch(DeliverUrl(route.Id, StopId(route, 2))), Ct);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "STOP_NOT_NEXT");
        Assert.All(factory.Routes.Get(route.Id)!.Stops, s => Assert.Equal("PENDING", s.Status));
    }

    [Fact]
    public async Task Deliver_WithoutToken_Returns401()
    {
        var route = TestRoutes.Eligible(factory.DriverId);
        factory.Routes.Reset(route);

        var response = await factory.CreateClient().SendAsync(Patch(DeliverUrl(route.Id, StopId(route, 1))), Ct);

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
        Assert.Equal(3, factory.Routes.Get(route.Id)!.Version);
    }

    [Fact]
    public async Task Deliver_AsManager_Returns403()
    {
        var route = TestRoutes.Eligible(factory.DriverId);
        factory.Routes.Reset(route);

        var response = await (await ClientAsync(SmartRouteApiFactory.ManagerEmail))
            .SendAsync(Patch(DeliverUrl(route.Id, StopId(route, 1))), Ct);

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "FORBIDDEN");
        Assert.Equal(3, factory.Routes.Get(route.Id)!.Version);
    }

    // ---------- Concurrence ----------

    [Fact]
    public async Task Deliver_ConcurrentRequestsOnSameStop_OnlyOneSucceeds()
    {
        var route = TestRoutes.Eligible(factory.DriverId);
        factory.Routes.Reset(route);
        var client = await DriverClientAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => client.SendAsync(Patch(DeliverUrl(route.Id, StopId(route, 1))), Ct)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        foreach (var refused in responses.Where(r => r.StatusCode != HttpStatusCode.OK))
        {
            await AssertProblemAsync(refused, HttpStatusCode.Conflict, "STOP_ALREADY_DELIVERED");
        }
        var stored = factory.Routes.Get(route.Id)!;
        Assert.Equal(4, stored.Version); // une seule écriture
        Assert.Equal(["DELIVERED", "PENDING", "PENDING"], stored.Stops.OrderBy(s => s.Order).Select(s => s.Status));
    }

    [Fact]
    public async Task Deliver_WhenUs2InsertionHappensBetweenReadAndWrite_KeepsBothChanges()
    {
        var route = TestRoutes.Eligible(factory.DriverId);
        factory.Routes.Reset(route);
        var driver = await DriverClientAsync();
        var manager = await ClientAsync(SmartRouteApiFactory.ManagerEmail);
        HttpStatusCode? confirmStatus = null;

        // La confirmation US2 s'écrit pendant la livraison, après sa lecture (version 3) et avant son écriture.
        factory.Routes.BeforeNextReplace = async () =>
        {
            var confirm = await manager.PostAsJsonAsync("/api/deliveries/confirm-assignment", new
            {
                address = new { label = TestRoutes.NewDeliveryLabel },
                routeId = route.Id,
                insertionPosition = 2,
                routeVersion = 3,
            }, Ct);
            confirmStatus = confirm.StatusCode;
        };

        var response = await driver.SendAsync(Patch(DeliverUrl(route.Id, StopId(route, 1))), Ct);

        Assert.Equal(HttpStatusCode.Created, confirmStatus);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // La livraison a été réappliquée sur la tournée modifiée : aucune des deux modifications n'est perdue.
        var stored = factory.Routes.Get(route.Id)!;
        Assert.Equal(5, stored.Version);
        Assert.Equal(
            ["Arret A", TestRoutes.NewDeliveryLabel, "Arret C", "Arret B"],
            stored.Stops.OrderBy(s => s.Order).Select(s => s.Address.Label));
        Assert.Equal(["DELIVERED", "PENDING", "PENDING", "PENDING"], stored.Stops.OrderBy(s => s.Order).Select(s => s.Status));
        Assert.Equal(4, (await JsonAsync(response)).GetProperty("stops").GetArrayLength());
    }

    [Fact]
    public async Task Us2Confirmation_ProposedBeforeADelivery_Returns409AndKeepsTheDelivery()
    {
        var route = TestRoutes.Eligible(factory.DriverId);
        factory.Routes.Reset(route);
        var manager = await ClientAsync(SmartRouteApiFactory.ManagerEmail);
        var proposal = await JsonAsync(await manager.PostAsJsonAsync(
            "/api/deliveries/assign-best-driver", new { address = new { label = TestRoutes.NewDeliveryLabel } }, Ct));

        var delivery = await (await DriverClientAsync()).SendAsync(Patch(DeliverUrl(route.Id, StopId(route, 1))), Ct);
        var confirm = await manager.PostAsJsonAsync("/api/deliveries/confirm-assignment", new
        {
            address = new { label = TestRoutes.NewDeliveryLabel },
            routeId = route.Id,
            insertionPosition = proposal.GetProperty("insertionPosition").GetInt32(),
            routeVersion = proposal.GetProperty("routeVersion").GetInt32(),
        }, Ct);

        Assert.Equal(HttpStatusCode.OK, delivery.StatusCode);
        await AssertProblemAsync(confirm, HttpStatusCode.Conflict, "PROPOSAL_OUTDATED");
        var stored = factory.Routes.Get(route.Id)!;
        Assert.Equal(4, stored.Version);
        Assert.Equal(3, stored.Stops.Count);
        Assert.Equal("DELIVERED", stored.Stops.Single(s => s.Order == 1).Status);
    }

    [Fact]
    public async Task Deliver_PersistenceFailure_Returns500WithoutTechnicalDetails()
    {
        var route = TestRoutes.Eligible(factory.DriverId);
        factory.Routes.Reset(route);
        factory.Routes.FailWrites = true;

        var response = await (await DriverClientAsync()).SendAsync(Patch(DeliverUrl(route.Id, StopId(route, 1))), Ct);

        factory.Routes.FailWrites = false;
        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError, "INTERNAL_ERROR");
        Assert.False(problem.TryGetProperty("exception", out _), "Aucun détail technique ne doit être exposé.");
        Assert.Equal(3, factory.Routes.Get(route.Id)!.Version);
    }
}
