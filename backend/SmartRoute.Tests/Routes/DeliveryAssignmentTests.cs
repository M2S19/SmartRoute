using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MongoDB.Bson;
using SmartRoute.Tests.Common;

namespace SmartRoute.Tests.Routes;

/// <summary>
/// US2 — POST /api/deliveries/assign-best-driver et /confirm-assignment,
/// sur tournées en mémoire (<see cref="InMemoryRouteRepository"/>) et géocodage simulé.
/// </summary>
public sealed class DeliveryAssignmentTests(SmartRouteApiFactory factory) : IClassFixture<SmartRouteApiFactory>
{
    private const string AssignUrl = "/api/deliveries/assign-best-driver";
    private const string ConfirmUrl = "/api/deliveries/confirm-assignment";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static object AssignRequest() => new { address = new { label = TestRoutes.NewDeliveryLabel } };

    private static object ConfirmRequest(string routeId, int insertionPosition, int routeVersion) => new
    {
        address = new { label = TestRoutes.NewDeliveryLabel },
        routeId,
        insertionPosition,
        routeVersion,
    };

    private async Task<HttpClient> ManagerClientAsync()
    {
        var client = factory.CreateClient();
        await TestAuth.AuthenticateAsync(client, SmartRouteApiFactory.ManagerEmail, Ct);
        return client;
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Ct);

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, (await JsonAsync(response)).GetProperty("code").GetString());
    }

    // ---------- Autorisations ----------

    [Theory]
    [InlineData(AssignUrl)]
    [InlineData(ConfirmUrl)]
    public async Task WithoutToken_Returns401(string url)
    {
        var response = await factory.CreateClient().PostAsJsonAsync(url, AssignRequest(), Ct);

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
    }

    [Theory]
    [InlineData(AssignUrl)]
    [InlineData(ConfirmUrl)]
    public async Task AsDriver_Returns403(string url)
    {
        var client = factory.CreateClient();
        await TestAuth.AuthenticateAsync(client, SmartRouteApiFactory.DriverEmail, Ct);

        var response = await client.PostAsJsonAsync(url, AssignRequest(), Ct);

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "FORBIDDEN");
    }

    // ---------- Proposition ----------

    [Fact]
    public async Task AssignBestDriver_AsManager_ReturnsProposalWithoutSaving()
    {
        var route = TestRoutes.Eligible(factory.DriverId);
        factory.Routes.Reset(route);

        var response = await (await ManagerClientAsync()).PostAsJsonAsync(AssignUrl, AssignRequest(), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await JsonAsync(response);
        Assert.Equal(
            ["driver", "extraDistanceKm", "insertionPosition", "routeId", "routeName", "routeVersion"],
            body.EnumerateObject().Select(p => p.Name).Order());
        Assert.Equal(route.Id, body.GetProperty("routeId").GetString());
        Assert.Equal("Tournée test", body.GetProperty("routeName").GetString());
        Assert.Equal(3, body.GetProperty("routeVersion").GetInt32());
        Assert.Equal(2, body.GetProperty("insertionPosition").GetInt32()); // entre A et C
        Assert.True(body.GetProperty("extraDistanceKm").GetDouble() >= 0);

        var driver = body.GetProperty("driver");
        Assert.Equal(factory.DriverId, driver.GetProperty("id").GetString());
        Assert.Equal("Pierre Dubois", driver.GetProperty("fullName").GetString());
        Assert.Equal("4092", driver.GetProperty("driverCode").GetString());

        // Rien n'est enregistré par une proposition.
        var stored = factory.Routes.Get(route.Id)!;
        Assert.Equal(3, stored.Stops.Count);
        Assert.Equal(3, stored.Version);
    }

    [Theory]
    [InlineData("COMPLETED", false)]
    [InlineData("ASSIGNED", true)] // tournée d'un autre jour
    public async Task AssignBestDriver_NoEligibleRoute_Returns422(string status, bool otherDay)
    {
        factory.Routes.Reset(TestRoutes.Eligible(factory.DriverId, date: otherDay ? "2000-01-01" : null, status: status));

        var response = await (await ManagerClientAsync()).PostAsJsonAsync(AssignUrl, AssignRequest(), Ct);

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "NO_ELIGIBLE_ROUTE");
    }

    [Fact]
    public async Task AssignBestDriver_WithoutAddress_Returns400()
    {
        var response = await (await ManagerClientAsync())
            .PostAsJsonAsync(AssignUrl, new { address = new { label = "" } }, Ct);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
    }

    // ---------- Confirmation ----------

    [Fact]
    public async Task Confirm_AsManager_SavesDeliveryAndReturns201()
    {
        var route = TestRoutes.Eligible(factory.DriverId);
        factory.Routes.Reset(route);
        var client = await ManagerClientAsync();

        var proposal = await JsonAsync(await client.PostAsJsonAsync(AssignUrl, AssignRequest(), Ct));
        var response = await client.PostAsJsonAsync(
            ConfirmUrl,
            ConfirmRequest(route.Id, proposal.GetProperty("insertionPosition").GetInt32(), proposal.GetProperty("routeVersion").GetInt32()),
            Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await JsonAsync(response);
        Assert.Equal(route.Id, body.GetProperty("routeId").GetString());
        Assert.Equal(4, body.GetProperty("routeVersion").GetInt32());
        Assert.Equal(2, body.GetProperty("insertionPosition").GetInt32());
        Assert.Equal("Pierre Dubois", body.GetProperty("driver").GetProperty("fullName").GetString());
        var stopId = body.GetProperty("stopId").GetString()!;
        Assert.True(ObjectId.TryParse(stopId, out _));

        // Enregistré : arrêt inséré en 2e position, ordre renuméroté 1..n, version incrémentée.
        var stored = factory.Routes.Get(route.Id)!;
        Assert.Equal(4, stored.Version);
        Assert.Equal(
            ["Arret A", TestRoutes.NewDeliveryLabel, "Arret C", "Arret B"],
            stored.Stops.OrderBy(s => s.Order).Select(s => s.Address.Label));
        Assert.Equal([1, 2, 3, 4], stored.Stops.OrderBy(s => s.Order).Select(s => s.Order));
        var created = stored.Stops.Single(s => s.Id == stopId);
        Assert.Equal("PENDING", created.Status);
        Assert.Equal(47.75, created.Address.Latitude, 6);
        Assert.Equal(7.315, created.Address.Longitude, 6);
    }

    [Fact]
    public async Task Confirm_UnknownRoute_Returns404()
    {
        factory.Routes.Reset();
        var client = await ManagerClientAsync();

        var unknown = await client.PostAsJsonAsync(ConfirmUrl, ConfirmRequest(ObjectId.GenerateNewId().ToString(), 2, 3), Ct);
        var malformed = await client.PostAsJsonAsync(ConfirmUrl, ConfirmRequest("pas-un-objectid", 2, 3), Ct);

        await AssertProblemAsync(unknown, HttpStatusCode.NotFound, "NOT_FOUND");
        await AssertProblemAsync(malformed, HttpStatusCode.NotFound, "NOT_FOUND");
    }

    [Fact]
    public async Task Confirm_RouteNoLongerEligible_Returns409AndSavesNothing()
    {
        var route = TestRoutes.Eligible(factory.DriverId, status: "COMPLETED");
        factory.Routes.Reset(route);

        var response = await (await ManagerClientAsync()).PostAsJsonAsync(ConfirmUrl, ConfirmRequest(route.Id, 2, 3), Ct);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "ROUTE_NOT_ELIGIBLE");
        Assert.Equal(3, factory.Routes.Get(route.Id)!.Stops.Count);
    }

    [Fact]
    public async Task Confirm_RouteChangedSinceProposal_Returns409AndSavesNothing()
    {
        var route = TestRoutes.Eligible(factory.DriverId);
        factory.Routes.Reset(route);
        var client = await ManagerClientAsync();
        var proposal = await JsonAsync(await client.PostAsJsonAsync(AssignUrl, AssignRequest(), Ct));

        // Entre-temps, la tournée est modifiée (ex. un arrêt livré) : sa version change.
        var changed = factory.Routes.Get(route.Id)!;
        changed.Stops.Single(s => s.Order == 1).Status = "DELIVERED";
        Assert.True(await factory.Routes.ReplaceIfVersionMatchesAsync(Bump(changed), expectedVersion: 3, Ct));

        var response = await client.PostAsJsonAsync(
            ConfirmUrl,
            ConfirmRequest(route.Id, proposal.GetProperty("insertionPosition").GetInt32(), proposal.GetProperty("routeVersion").GetInt32()),
            Ct);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "PROPOSAL_OUTDATED");
        Assert.Equal(3, factory.Routes.Get(route.Id)!.Stops.Count);
    }

    [Fact]
    public async Task Confirm_WrongInsertionPosition_Returns409()
    {
        var route = TestRoutes.Eligible(factory.DriverId);
        factory.Routes.Reset(route);

        // Position falsifiée par le client : le serveur recalcule et refuse.
        var response = await (await ManagerClientAsync()).PostAsJsonAsync(ConfirmUrl, ConfirmRequest(route.Id, 4, 3), Ct);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "PROPOSAL_OUTDATED");
        Assert.Equal(3, factory.Routes.Get(route.Id)!.Stops.Count);
    }

    [Fact]
    public async Task Confirm_Twice_SecondReturns409AndDeliveryIsAddedOnce()
    {
        var route = TestRoutes.Eligible(factory.DriverId);
        factory.Routes.Reset(route);
        var client = await ManagerClientAsync();
        var request = ConfirmRequest(route.Id, 2, 3);

        var first = await client.PostAsJsonAsync(ConfirmUrl, request, Ct);
        var second = await client.PostAsJsonAsync(ConfirmUrl, request, Ct);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        await AssertProblemAsync(second, HttpStatusCode.Conflict, "PROPOSAL_OUTDATED");
        var stored = factory.Routes.Get(route.Id)!;
        Assert.Equal(4, stored.Stops.Count);
        Assert.Single(stored.Stops, s => s.Address.Label == TestRoutes.NewDeliveryLabel);
    }

    [Fact]
    public async Task Confirm_ConcurrentRequests_OnlyOneSucceeds()
    {
        var route = TestRoutes.Eligible(factory.DriverId);
        factory.Routes.Reset(route);
        var client = await ManagerClientAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => client.PostAsJsonAsync(ConfirmUrl, ConfirmRequest(route.Id, 2, 3), Ct)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        Assert.Single(factory.Routes.Get(route.Id)!.Stops, s => s.Address.Label == TestRoutes.NewDeliveryLabel);
    }

    [Fact]
    public async Task Confirm_PersistenceFailure_Returns500AndSavesNothing()
    {
        var route = TestRoutes.Eligible(factory.DriverId);
        factory.Routes.Reset(route);
        factory.Routes.FailWrites = true;

        var response = await (await ManagerClientAsync()).PostAsJsonAsync(ConfirmUrl, ConfirmRequest(route.Id, 2, 3), Ct);

        factory.Routes.FailWrites = false;
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var problem = await JsonAsync(response);
        Assert.Equal("INTERNAL_ERROR", problem.GetProperty("code").GetString());
        Assert.False(problem.TryGetProperty("exception", out _), "Aucun détail technique ne doit être exposé.");
        var stored = factory.Routes.Get(route.Id)!;
        Assert.Equal(3, stored.Stops.Count);
        Assert.Equal(3, stored.Version);
    }

    [Fact]
    public async Task Confirm_InvalidBody_Returns400()
    {
        var response = await (await ManagerClientAsync())
            .PostAsJsonAsync(ConfirmUrl, new { address = new { label = TestRoutes.NewDeliveryLabel }, insertionPosition = 0 }, Ct);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
    }

    private static SmartRoute.Api.Models.Route Bump(SmartRoute.Api.Models.Route route)
    {
        route.Version++;
        return route;
    }
}
