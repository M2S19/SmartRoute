using SmartRoute.Core.Entities;

namespace SmartRoute.Core.Interfaces;

public interface IUserRepository
{
    /// <param name="normalizedEmail">Email déjà normalisé (<see cref="User.NormalizeEmail"/>).</param>
    Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    /// <summary>Enregistre l'utilisateur et renseigne son <see cref="User.Id"/>.</summary>
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
