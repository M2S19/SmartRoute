using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SmartRoute.Api.Models;
using SmartRoute.Api.Settings;

namespace SmartRoute.Api.Auth;

public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);

/// <summary>Émet les JWT (HS256) avec les claims sub, email, role.</summary>
public sealed class JwtTokenService(IOptions<JwtSettings> options, TimeProvider timeProvider)
{
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken CreateToken(User user)
    {
        var settings = options.Value;

        // Tronqué à la seconde : identique au claim « exp » et à « expiresAt » renvoyé au front.
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var issuedAt = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        var expiresAt = issuedAt.AddHours(settings.LifetimeHours);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = expiresAt,
            Claims = new Dictionary<string, object>
            {
                [ClaimNames.Subject] = user.Id,
                [ClaimNames.Email] = user.Email,
                [ClaimNames.Role] = user.Role,
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(settings.SigningKeyBytes),
                SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(_handler.CreateToken(descriptor), expiresAt);
    }
}
