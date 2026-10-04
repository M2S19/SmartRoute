using SmartRoute.Api.Auth;
using SmartRoute.Api.DTOs.Auth;
using SmartRoute.Api.Errors;
using SmartRoute.Api.Models;
using SmartRoute.Api.Repositories;

namespace SmartRoute.Api.Services;

public sealed class AuthService(UserRepository users, PasswordHasher passwordHasher, JwtTokenService tokenService)
{
    /// <exception cref="ApiException">
    /// 401 INVALID_CREDENTIALS : email inconnu ou mot de passe faux, même réponse dans les deux cas
    /// (on ne révèle pas quels comptes existent).
    /// </exception>
    public async Task<LoginResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByEmailAsync(User.NormalizeEmail(email), cancellationToken);

        if (user is null || !passwordHasher.Verify(user, password))
        {
            throw new ApiException(
                StatusCodes.Status401Unauthorized,
                ErrorCodes.InvalidCredentials,
                "Email ou mot de passe incorrect.");
        }

        var token = tokenService.CreateToken(user);
        return new LoginResponse(token.Token, token.ExpiresAtUtc, UserDto.From(user));
    }
}
