using SmartRoute.Core.Entities;
using SmartRoute.Core.Exceptions;
using SmartRoute.Core.Interfaces;

namespace SmartRoute.Core.Services;

public sealed record LoginResult(AccessToken AccessToken, User User);

public sealed class AuthService(IUserRepository users, IPasswordHasher passwordHasher, ITokenService tokenService)
{
    /// <exception cref="InvalidCredentialsException">
    /// Email inconnu ou mot de passe faux : même erreur dans les deux cas (pas d'énumération des comptes).
    /// </exception>
    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByEmailAsync(User.NormalizeEmail(email), cancellationToken);

        if (user is null || !passwordHasher.Verify(user.PasswordHash, password))
        {
            throw new InvalidCredentialsException();
        }

        return new LoginResult(tokenService.CreateToken(user), user);
    }
}
