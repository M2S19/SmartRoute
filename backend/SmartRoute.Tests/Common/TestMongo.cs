using MongoDB.Bson;
using MongoDB.Driver;

namespace SmartRoute.Tests.Common;

/// <summary>
/// MongoDB réel pour les tests d'intégration (base « smartroute_test »).
/// - SMARTROUTE_TEST_MONGO : chaîne de connexion (défaut : mongodb://localhost:27017).
/// - SMARTROUTE_REQUIRE_MONGO=true : MongoDB obligatoire, le test échoue s'il est absent (CI).
///   Sinon, le test est ignoré quand MongoDB n'est pas joignable (exécution locale sans Docker).
/// </summary>
public static class TestMongo
{
    public const string DatabaseName = "smartroute_test";

    /// <summary>
    /// Collection xUnit des tests qui vident et remplissent la collection « routes » :
    /// exécutés l'un après l'autre (xUnit exécute sinon les classes de test en parallèle).
    /// </summary>
    public const string CollectionName = "MongoDB";

    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("SMARTROUTE_TEST_MONGO") is { Length: > 0 } value
            ? value
            : "mongodb://localhost:27017";

    private static bool IsRequired =>
        string.Equals(Environment.GetEnvironmentVariable("SMARTROUTE_REQUIRE_MONGO"), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>Configuration à injecter dans l'API pour qu'elle utilise ce MongoDB.</summary>
    public static Dictionary<string, string?> ApiSettings => new()
    {
        ["MongoDb:ConnectionString"] = ConnectionString,
        ["MongoDb:DatabaseName"] = DatabaseName,
    };

    /// <summary>Ignore le test si MongoDB est injoignable, sauf si SMARTROUTE_REQUIRE_MONGO=true.</summary>
    public static async Task EnsureAvailableAsync(CancellationToken cancellationToken)
    {
        var settings = MongoClientSettings.FromConnectionString(ConnectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(2);

        try
        {
            await new MongoClient(settings).GetDatabase(DatabaseName)
                .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
        }
        catch (Exception exception) when (exception is TimeoutException or MongoException)
        {
            var message = $"MongoDB injoignable sur {ConnectionString} ({exception.GetType().Name}).";
            if (IsRequired) Assert.Fail(message + " SMARTROUTE_REQUIRE_MONGO=true : MongoDB est obligatoire.");
            Assert.Skip(message + " Lancez MongoDB (docker compose) pour exécuter ce test.");
        }
    }
}
