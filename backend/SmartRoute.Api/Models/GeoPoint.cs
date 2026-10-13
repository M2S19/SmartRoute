using MongoDB.Bson.Serialization.Attributes;

namespace SmartRoute.Api.Models;

public class GeoPoint
{
    [BsonElement("label")]
    public required string Label { get; set; }

    [BsonElement("latitude")]
    public double Latitude { get; set; }

    [BsonElement("longitude")]
    public double Longitude { get; set; }
}