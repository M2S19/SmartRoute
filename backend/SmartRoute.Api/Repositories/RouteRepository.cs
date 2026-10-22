using MongoDB.Bson;
using MongoDB.Driver;
using RouteModel = SmartRoute.Api.Models.Route;

namespace SmartRoute.Api.Repositories;

/// <summary>
/// Accès à la collection « routes ».
/// Méthodes virtuelles : les tests les remplacent par une version en mémoire (sans MongoDB).
/// </summary>
public class RouteRepository(IMongoDatabase database)
{
    public const string CollectionName = "routes";

    private readonly IMongoCollection<RouteModel> _routes =
        database.GetCollection<RouteModel>(CollectionName);

    /// <summary>
    /// Récupère les tournées pouvant recevoir une nouvelle livraison :
    /// - tournée du jour demandé
    /// - statut ASSIGNED ou IN_PROGRESS
    /// - livreur affecté
    /// </summary>
    public virtual async Task<List<RouteModel>> FindEligibleRoutesAsync(
        string date,
        CancellationToken cancellationToken = default)
    {
        var dateFilter =
            Builders<RouteModel>.Filter.Eq(
                route => route.Date,
                date
            );

        var statusFilter =
            Builders<RouteModel>.Filter.In(
                route => route.Status,
                RouteModel.StatusesAcceptingDeliveries
            );

        var assignedDriverFilter =
            Builders<RouteModel>.Filter.And(
                Builders<RouteModel>.Filter.Ne(
                    route => route.AssignedDriverId,
                    null
                ),
                Builders<RouteModel>.Filter.Ne(
                    route => route.AssignedDriverId,
                    string.Empty
                )
            );

        var filter =
            Builders<RouteModel>.Filter.And(
                dateFilter,
                statusFilter,
                assignedDriverFilter
            );

        return await _routes
            .Find(filter)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Tournées affectées à un livreur, de la plus récente à la plus ancienne (US3).</summary>
    public virtual async Task<List<RouteModel>> FindByDriverAsync(
        string driverId,
        CancellationToken cancellationToken = default)
    {
        return await _routes
            .Find(route => route.AssignedDriverId == driverId)
            .SortByDescending(route => route.Date)
            .ThenBy(route => route.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Tournée par identifiant ; null si l'identifiant n'est pas un ObjectId valide ou n'existe pas.</summary>
    public virtual async Task<RouteModel?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _)) return null;

        return await _routes.Find(route => route.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Remplace la tournée seulement si sa version en base vaut encore <paramref name="expectedVersion"/>
    /// (concurrence optimiste, filtre { _id, version }). Opération atomique sur un seul document.
    /// </summary>
    /// <returns>false si la tournée a été modifiée entre-temps (ou n'existe plus) : rien n'est écrit.</returns>
    public virtual async Task<bool> ReplaceIfVersionMatchesAsync(
        RouteModel route,
        int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<RouteModel>.Filter.And(
            Builders<RouteModel>.Filter.Eq(r => r.Id, route.Id),
            Builders<RouteModel>.Filter.Eq(r => r.Version, expectedVersion));

        ReplaceOneResult result = await _routes.ReplaceOneAsync(filter, route, cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }

    /// <summary>Insère une tournée ; MongoDB renseigne son <see cref="RouteModel.Id"/>.</summary>
    public virtual Task InsertAsync(RouteModel route, CancellationToken cancellationToken = default)
    {
        return _routes.InsertOneAsync(route, cancellationToken: cancellationToken);
    }

    /// <summary>Vrai si au moins une tournée existe pour cette date (seed de démonstration).</summary>
    public virtual async Task<bool> ExistsForDateAsync(string date, CancellationToken cancellationToken = default)
    {
        return await _routes.Find(route => route.Date == date).AnyAsync(cancellationToken);
    }
}
