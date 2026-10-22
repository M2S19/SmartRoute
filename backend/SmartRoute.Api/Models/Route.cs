using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartRoute.Api.Models;

[BsonIgnoreExtraElements]
public class Route
{
    /// <summary>Statuts d'une tournée qui peut encore recevoir une nouvelle livraison.</summary>
    public static readonly string[] StatusesAcceptingDeliveries = ["ASSIGNED", "IN_PROGRESS"];

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public required string Name { get; set; }

    [BsonElement("date")]
    public required string Date { get; set; }

    [BsonElement("status")]
    public required string Status { get; set; }

    [BsonElement("version")]
    public int Version { get; set; }

    [BsonElement("depot")]
    public required GeoPoint Depot { get; set; }

    [BsonElement("assignedDriverId")]
    [BsonIgnoreIfNull]
    public string? AssignedDriverId { get; set; }

    [BsonElement("stops")]
    public List<RouteStop> Stops { get; set; } = new();
}