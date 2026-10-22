using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SmartRoute.Tests.Common;

namespace SmartRoute.Tests.Routes;

/// <summary>
/// POST /api/routes/optimize : autorisations et contrat JSON (géocodage simulé, voir <see cref="FakeGeocodingHandler"/>).
/// </summary>
public sealed class OptimizeRouteTests(SmartRouteApiFactory factory) : IClassFixture<SmartRouteApiFactory>
{
    private const string OptimizeUrl = "/api/routes/optimize";
    private const string Depot = "Depot Mulhouse";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // Ordre saisi volontairement mauvais (B, A, C) ; points alignés : dépôt 7.30, A 7.31, C 7.32, B 7.33.
    private static object ValidRequest() => new
    {
        depot = new { label = Depot },
        stops = new[] { new { label = "Arret B" }, new { label = "Arret A" }, new { label = "Arret C" } },
    };

    [Fact]
    public async Task Optimize_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(OptimizeUrl, ValidRequest(), Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("UNAUTHORIZED", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Optimize_AsDriver_Returns403()
    {
        var client = factory.CreateClient();
        await TestAuth.AuthenticateAsync(client, SmartRouteApiFactory.DriverEmail, Ct);

        var response = await client.PostAsJsonAsync(OptimizeUrl, ValidRequest(), Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("FORBIDDEN", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Optimize_AsManager_Returns200WithContractUsedByTheMobileApp()
    {
        var client = factory.CreateClient();
        await TestAuth.AuthenticateAsync(client, SmartRouteApiFactory.ManagerEmail, Ct);

        var response = await client.PostAsJsonAsync(OptimizeUrl, ValidRequest(), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);

        // Noms JSON lus par frontend/src/services/route/routeOptimizationMapper.ts.
        Assert.Equal(
            ["distanceAfterKm", "distanceBeforeKm", "gainPercent", "optimizedOrder"],
            body.EnumerateObject().Select(p => p.Name).Order());

        var order = body.GetProperty("optimizedOrder").EnumerateArray().ToList();
        Assert.All(order, point => Assert.Equal(
            ["label", "latitude", "longitude", "position"],
            point.EnumerateObject().Select(p => p.Name).Order()));

        // Dépôt au début et à la fin ; positions consécutives à partir de 0.
        Assert.Equal(5, order.Count);
        Assert.Equal(Depot, order[0].GetProperty("label").GetString());
        Assert.Equal(Depot, order[^1].GetProperty("label").GetString());
        Assert.Equal([0, 1, 2, 3, 4], order.Select(p => p.GetProperty("position").GetInt32()));

        // Ordre optimisé attendu pour ces points alignés : A, C, B.
        Assert.Equal(
            ["Arret A", "Arret C", "Arret B"],
            order.Skip(1).Take(3).Select(p => p.GetProperty("label").GetString()));
        Assert.Equal(47.75, order[1].GetProperty("latitude").GetDouble(), 6);
        Assert.Equal(7.31, order[1].GetProperty("longitude").GetDouble(), 6);

        var before = body.GetProperty("distanceBeforeKm").GetDouble();
        var after = body.GetProperty("distanceAfterKm").GetDouble();
        Assert.True(after < before, $"Distance optimisée ({after}) attendue inférieure à l'initiale ({before}).");
        Assert.Equal((before - after) / before * 100, body.GetProperty("gainPercent").GetDouble(), 6);
    }

    /// <summary>
    /// Comportement actuel du backend, signalé comme anomalie : les erreurs métier de l'optimisation
    /// (aucun arrêt, adresse inconnue, géocodage indisponible) sortent en 500 générique.
    /// Ce test vérifie au moins qu'aucun détail technique n'est exposé.
    /// </summary>
    [Theory]
    [InlineData("aucun-arret")]
    [InlineData("adresse-inconnue")]
    [InlineData("geocodage-indisponible")]
    public async Task Optimize_BusinessError_CurrentlyReturnsGeneric500(string scenario)
    {
        var client = factory.CreateClient();
        await TestAuth.AuthenticateAsync(client, SmartRouteApiFactory.ManagerEmail, Ct);

        object request = scenario switch
        {
            "aucun-arret" => new { depot = new { label = Depot }, stops = Array.Empty<object>() },
            "adresse-inconnue" => new { depot = new { label = Depot }, stops = new[] { new { label = "Adresse qui n'existe pas" } } },
            _ => new { depot = new { label = Depot }, stops = new[] { new { label = FakeGeocodingHandler.OutageKeyword } } },
        };

        var response = await client.PostAsJsonAsync(OptimizeUrl, request, Ct);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("INTERNAL_ERROR", problem.GetProperty("code").GetString());
        Assert.Equal("Une erreur interne est survenue.", problem.GetProperty("detail").GetString());
        Assert.False(problem.TryGetProperty("exception", out _), "Aucun détail technique ne doit être exposé.");
    }
}
