using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using SmartRoute.Core.Entities;
using SmartRoute.Core.Enums;

namespace SmartRoute.Infrastructure.Mongo;

/// <summary>
/// Correspondance entités ↔ documents MongoDB, déclarée ici pour garder Core sans dépendance Mongo.
/// </summary>
public static class MongoMappings
{
    private static readonly Lock RegistrationLock = new();
    private static bool _registered;

    public static void Register()
    {
        lock (RegistrationLock)
        {
            if (_registered) return;

            ConventionRegistry.Register(
                "SmartRoute",
                new ConventionPack { new CamelCaseElementNameConvention(), new IgnoreExtraElementsConvention(true) },
                type => type.Namespace?.StartsWith("SmartRoute", StringComparison.Ordinal) == true);

            BsonClassMap.RegisterClassMap<User>(map =>
            {
                map.AutoMap();
                // Id exposé en string, stocké en ObjectId, généré à l'insertion.
                map.MapIdMember(u => u.Id)
                    .SetIdGenerator(StringObjectIdGenerator.Instance)
                    .SetSerializer(new StringSerializer(BsonType.ObjectId));
                map.MapMember(u => u.Role).SetSerializer(new UserRoleSerializer());
                map.MapMember(u => u.DriverCode).SetIgnoreIfNull(true);
            });

            _registered = true;
        }
    }

    /// <summary>Stocke le rôle sous sa forme texte (« LIVREUR »), comme dans le JWT et le JSON.</summary>
    private sealed class UserRoleSerializer : SerializerBase<UserRole>
    {
        public override UserRole Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var name = context.Reader.ReadString();
            return UserRoleNames.TryParse(name, out var role)
                ? role
                : throw new FormatException($"Rôle inconnu en base : « {name} ».");
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, UserRole value)
        {
            context.Writer.WriteString(value.ToName());
        }
    }
}
