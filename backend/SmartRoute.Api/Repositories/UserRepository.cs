using MongoDB.Driver;
using SmartRoute.Api.Models;

namespace SmartRoute.Api.Repositories;

/// <summary>
/// Accès à la collection « users ».
/// Méthodes virtuelles : les tests les remplacent par une version en mémoire (sans MongoDB).
/// </summary>
public class UserRepository(IMongoDatabase database)
{
    public const string CollectionName = "users";

    private readonly IMongoCollection<User> _users = database.GetCollection<User>(CollectionName);

    /// <param name="normalizedEmail">Email déjà normalisé (<see cref="User.NormalizeEmail"/>).</param>
    public virtual async Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return await _users.Find(u => u.Email == normalizedEmail).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Insère l'utilisateur ; MongoDB renseigne son <see cref="User.Id"/>.</summary>
    public virtual Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        return _users.InsertOneAsync(user, cancellationToken: cancellationToken);
    }

    /// <summary>Index unique sur l'email. Idempotent : appelé à chaque démarrage.</summary>
    public virtual Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        var uniqueEmail = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true, Name = "ux_users_email" });

        return _users.Indexes.CreateOneAsync(uniqueEmail, cancellationToken: cancellationToken);
    }

    public virtual async Task<User?> FindByIdAsync(
    string id,
    CancellationToken cancellationToken = default)
    {
        return await _users
            .Find(user => user.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
