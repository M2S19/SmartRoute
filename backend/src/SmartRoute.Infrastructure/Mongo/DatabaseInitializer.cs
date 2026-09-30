using MongoDB.Driver;
using SmartRoute.Core.Entities;

namespace SmartRoute.Infrastructure.Mongo;

/// <summary>Crée les index nécessaires. Idempotent : peut être appelé à chaque démarrage.</summary>
public sealed class DatabaseInitializer(IMongoDatabase database)
{
    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        var users = database.GetCollection<User>(MongoCollections.Users);
        var uniqueEmail = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true, Name = "ux_users_email" });

        await users.Indexes.CreateOneAsync(uniqueEmail, cancellationToken: cancellationToken);
    }
}
