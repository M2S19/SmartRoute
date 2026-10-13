using MongoDB.Bson.Serialization.Attributes;

namespace SmartRoute.Api.Models;

public class RouteStop
{
    [BsonElement("id")]
    public required string Id { get; set; }

    [BsonElement("order")]
    public int Order { get; set; }

    [BsonElement("status")]
    public required string Status { get; set; }

    [BsonElement("address")]
    public required GeoPoint Address { get; set; }

    [BsonElement("deliveredAt")]
    [BsonIgnoreIfNull]
    public DateTime? DeliveredAt { get; set; }
}