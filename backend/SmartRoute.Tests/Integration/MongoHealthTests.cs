using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using SmartRoute.Tests.Common;

namespace SmartRoute.Tests.Integration;

/// <summary>Test d'intégration sur un vrai MongoDB (voir <see cref="TestMongo"/>).</summary>
public sealed class MongoHealthTests(SmartRouteApiFactory factory) : IClassFixture<SmartRouteApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task MongoHealth_WhenMongoIsReachable_Returns200()
    {
        await TestMongo.EnsureAvailableAsync(Ct);

        var client = factory
            .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration(config =>
                config.AddInMemoryCollection(TestMongo.ApiSettings)))
            .CreateClient();

        var response = await client.GetAsync("/api/mongo-health", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("ok", body.GetProperty("status").GetString());
        Assert.Equal("ok", body.GetProperty("mongo").GetString());
    }
}
