using MongoDB.Driver;
using SmartRoute.Core.Entities;
using SmartRoute.Core.Interfaces;
using SmartRoute.Infrastructure.Mongo;

namespace SmartRoute.Infrastructure.Repositories;

public sealed class UserRepository(IMongoDatabase database) : IUserRepository
{
    private readonly IMongoCollection<User> _users = database.GetCollection<User>(MongoCollections.Users);

    public async Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return await _users.Find(u => u.Email == normalizedEmail).FirstOrDefaultAsync(cancellationToken);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        return _users.InsertOneAsync(user, cancellationToken: cancellationToken);
    }
}
