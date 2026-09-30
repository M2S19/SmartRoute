using Microsoft.AspNetCore.Identity;
using SmartRoute.Api.Auth;
using SmartRoute.Api.Errors;
using SmartRoute.Api.Models;
using SmartRoute.Api.Repositories;

namespace SmartRoute.Api.Services;

public sealed record LoginResult(AccessToken AccessToken, User User);

public sealed class AuthService(UserRepository users, PasswordHasher<User> passwordHasher, JwtTokenService tokenService)
{
    /// <exception cref="ApiException">
    /// 401 INVALID_CREDENTIALS : email inconnu ou mot de passe faux, même réponse dans les deux cas
    /// (on ne révèle pas quels comptes existent).
    /// </exception>
    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByEmailAsync(User.NormalizeEmail(email), cancellationToken);

        if (user is null || !IsPasswordValid(user, password))
        {
            throw new ApiException(
                StatusCodes.Status401Unauthorized,
                ErrorCodes.InvalidCredentials,
                "Email ou mot de passe incorrect.");
        }

        return new LoginResult(tokenService.CreateToken(user), user);
    }

    private bool IsPasswordValid(User user, string password)
    {
        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
