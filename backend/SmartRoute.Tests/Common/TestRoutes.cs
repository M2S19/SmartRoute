using System.Globalization;
using MongoDB.Bson;
using SmartRoute.Api.Models;
using RouteModel = SmartRoute.Api.Models.Route;

namespace SmartRoute.Tests.Common;

/// <summary>Tournées de test, cohérentes avec les adresses de <see cref="FakeGeocodingHandler"/>.</summary>
public static class TestRoutes
{
    public const string NewDeliveryLabel = "Nouvelle livraison";

    /// <summary>Jour de la tournée, calculé comme l'API (heure locale du serveur).</summary>
    public static string Today() => DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Tournée du jour Dépôt Sud → A → C → B, affectée au livreur, aucun arrêt livré.</summary>
    public static RouteModel Eligible(string driverId, string? date = null, string status = "ASSIGNED", int version = 3) => new()
    {
        Id = ObjectId.GenerateNewId().ToString(),
        Name = "Tournée test",
        Date = date ?? Today(),
        Status = status,
        Version = version,
        AssignedDriverId = driverId,
        Depot = Point("Depot Sud"),
        Stops =
        [
            Stop(1, "Arret A"),
            Stop(2, "Arret C"),
            Stop(3, "Arret B"),
        ],
    };

    private static RouteStop Stop(int order, string label) => new()
    {
        Id = ObjectId.GenerateNewId().ToString(),
        Order = order,
        Status = "PENDING",
        Address = Point(label),
    };

    private static GeoPoint Point(string label)
    {
        var (latitude, longitude) = FakeGeocodingHandler.KnownAddresses[label];
        return new GeoPoint { Label = label, Latitude = latitude, Longitude = longitude };
    }
}
