using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using SmartRoute.Tests.Common;

namespace SmartRoute.Tests.Integration;

/// <summary>GET /api/mongo-health : 200 si MongoDB répond, 500 sinon.</summary>
public sealed class MongoHealthTests(SmartRouteApiFactory factory) : IClassFixture<SmartRouteApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Cas nominal sur un vrai MongoDB (voir <see cref="TestMongo"/> ; obligatoire en CI).</summary>
    [Fact]
    public async Task MongoHealth_WhenMongoIsReachable_Returns200()
    {
        await TestMongo.EnsureAvailableAsync(Ct);

        var client = CreateClient(TestMongo.ApiSettings);
        var response = await client.GetAsync("/api/mongo-health", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("MongoDB connection successful", body.GetProperty("status").GetString());
    }

    /// <summary>MongoDB injoignable : aucun MongoDB requis pour ce test.</summary>
    [Fact]
    public async Task MongoHealth_WhenMongoIsUnreachable_Returns500()
    {
        var client = CreateClient(new Dictionary<string, string?>
        {
            // Port sans serveur ; délai de sélection court pour ne pas attendre les 30 s par défaut du driver.
            ["MongoDb:ConnectionString"] = "mongodb://127.0.0.1:27999/?serverSelectionTimeoutMS=1000",
            ["MongoDb:DatabaseName"] = TestMongo.DatabaseName,
        });

        var response = await client.GetAsync("/api/mongo-health", Ct);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("MongoDB connection failed", body.GetProperty("status").GetString());
    }

    private HttpClient CreateClient(Dictionary<string, string?> settings) =>
        factory
            .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(settings)))
            .CreateClient();
}
