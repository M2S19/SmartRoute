using MongoDB.Driver;
using RouteModel = SmartRoute.Api.Models.Route;

namespace SmartRoute.Api.Repositories;

/// <summary>
/// Accès à la collection « routes ».
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
                new[] { "ASSIGNED", "IN_PROGRESS" }
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
}