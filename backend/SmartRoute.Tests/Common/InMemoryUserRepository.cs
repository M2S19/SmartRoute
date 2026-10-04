using System.Collections.Concurrent;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartRoute.Api.Models;
using SmartRoute.Api.Repositories;

namespace SmartRoute.Tests.Common;

/// <summary>
/// Remplace UserRepository dans les tests d'API : aucune base MongoDB n'est nécessaire.
/// </summary>
public sealed class InMemoryUserRepository() : UserRepository(UnusedDatabase)
{
    // Le client MongoDB ne se connecte qu'à la première requête, qui n'a jamais lieu ici.
    private static readonly IMongoDatabase UnusedDatabase =
        new MongoClient("mongodb://unused.invalid:27017").GetDatabase("unused");

    private readonly ConcurrentDictionary<string, User> _usersByEmail = new();

    public override Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_usersByEmail.GetValueOrDefault(normalizedEmail));
    }

    public override Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(user.Id)) user.Id = ObjectId.GenerateNewId().ToString();
        _usersByEmail[user.Email] = user;
        return Task.CompletedTask;
    }

    public override Task EnsureIndexesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
