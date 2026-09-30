using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartRoute.Api.Models;

/// <summary>Document de la collection « users ».</summary>
[BsonIgnoreExtraElements]
public class User
{
    /// <summary>ObjectId MongoDB exposé en string ; généré à l'insertion.</summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>Toujours normalisé (<see cref="NormalizeEmail"/>). Index unique.</summary>
    [BsonElement("email")]
    public required string Email { get; set; }

    [BsonElement("fullName")]
    public required string FullName { get; set; }

    [BsonElement("passwordHash")]
    public required string PasswordHash { get; set; }

    /// <summary>« RESPONSABLE_LOGISTIQUE » ou « LIVREUR » (voir <see cref="Auth.Roles"/>).</summary>
    [BsonElement("role")]
    public required string Role { get; set; }

    /// <summary>Identifiant affiché d'un livreur, ex. « 4092 ».</summary>
    [BsonElement("driverCode")]
    [BsonIgnoreIfNull]
    public string? DriverCode { get; set; }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
