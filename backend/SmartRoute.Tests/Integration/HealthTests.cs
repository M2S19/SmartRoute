using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SmartRoute.Tests.Common;

namespace SmartRoute.Tests.Integration;

public sealed class HealthTests(SmartRouteApiFactory factory) : IClassFixture<SmartRouteApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Health_WithoutToken_Returns200WithoutCallingMongo()
    {
        // La factory pointe vers une base jamais contactée : /api/health ne dépend pas de MongoDB.
        var response = await factory.CreateClient().GetAsync("/api/health", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("ok", body.GetProperty("status").GetString());
    }
}
