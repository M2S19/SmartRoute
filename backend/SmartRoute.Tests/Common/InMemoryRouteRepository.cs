using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using SmartRoute.Api.Repositories;
using RouteModel = SmartRoute.Api.Models.Route;

namespace SmartRoute.Tests.Common;

/// <summary>
/// Remplace RouteRepository dans les tests d'API : tournées en mémoire, mêmes règles que MongoDB.
/// Les tournées sont stockées et rendues sous forme de copies (comme une base) : modifier un objet lu
/// ne change rien tant qu'il n'est pas réécrit.
/// </summary>
public sealed class InMemoryRouteRepository() : RouteRepository(UnusedDatabase)
{
    private static readonly IMongoDatabase UnusedDatabase =
        new MongoClient("mongodb://unused.invalid:27017").GetDatabase("unused");

    private readonly Dictionary<string, BsonDocument> _routes = new();
    private readonly Lock _lock = new();

    /// <summary>Simule une panne MongoDB lors de l'écriture d'une tournée.</summary>
    public bool FailWrites { get; set; }

    /// <summary>
    /// Exécuté une seule fois juste avant la prochaine écriture conditionnelle : simule une écriture concurrente
    /// arrivée entre la lecture et l'écriture d'une requête (ex. confirmation US2 pendant une livraison US3).
    /// </summary>
    public Func<Task>? BeforeNextReplace
    {
        get => _beforeNextReplace;
        set => _beforeNextReplace = value;
    }

    private Func<Task>? _beforeNextReplace;

    /// <summary>Remplace le contenu par ces tournées (identifiants générés si absents).</summary>
    public void Reset(params RouteModel[] routes)
    {
        lock (_lock)
        {
            _routes.Clear();
            FailWrites = false;
            BeforeNextReplace = null;
            foreach (var route in routes)
            {
                if (string.IsNullOrEmpty(route.Id)) route.Id = ObjectId.GenerateNewId().ToString();
                _routes[route.Id] = route.ToBsonDocument();
            }
        }
    }

    /// <summary>État actuellement « en base » d'une tournée.</summary>
    public RouteModel? Get(string id)
    {
        lock (_lock) return _routes.TryGetValue(id, out var doc) ? Clone(doc) : null;
    }

    public override Task<List<RouteModel>> FindEligibleRoutesAsync(string date, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_routes.Values
                .Select(Clone)
                .Where(route => route.Date == date
                                && RouteModel.StatusesAcceptingDeliveries.Contains(route.Status)
                                && !string.IsNullOrEmpty(route.AssignedDriverId))
                .ToList());
        }
    }

    public override Task<RouteModel?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Get(id));
    }

    public override Task<List<RouteModel>> FindByDriverAsync(string driverId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_routes.Values
                .Select(Clone)
                .Where(route => route.AssignedDriverId == driverId)
                .OrderByDescending(route => route.Date, StringComparer.Ordinal)
                .ThenBy(route => route.Name, StringComparer.Ordinal)
                .ToList());
        }
    }

    public override async Task<bool> ReplaceIfVersionMatchesAsync(
        RouteModel route,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        Func<Task>? concurrentWrite = Interlocked.Exchange(ref _beforeNextReplace, null);
        if (concurrentWrite is not null) await concurrentWrite();

        lock (_lock)
        {
            if (FailWrites) throw new MongoException("Écriture MongoDB simulée en échec.");
            if (!_routes.TryGetValue(route.Id, out var current) || Clone(current).Version != expectedVersion)
            {
                return false;
            }

            _routes[route.Id] = route.ToBsonDocument();
            return true;
        }
    }

    public override Task InsertAsync(RouteModel route, CancellationToken cancellationToken = default)
    {
        Reset([.. _routes.Values.Select(Clone), route]);
        return Task.CompletedTask;
    }

    public override Task<bool> ExistsForDateAsync(string date, CancellationToken cancellationToken = default)
    {
        lock (_lock) return Task.FromResult(_routes.Values.Select(Clone).Any(route => route.Date == date));
    }

    private static RouteModel Clone(BsonDocument document) => BsonSerializer.Deserialize<RouteModel>(document);
}
