using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SmartRoute.Core.Entities;
using SmartRoute.Core.Enums;
using SmartRoute.Core.Interfaces;

namespace SmartRoute.Infrastructure.Security;

public sealed class JwtTokenService(IOptions<JwtSettings> options, TimeProvider timeProvider) : ITokenService
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
                [JwtRegisteredClaimNames.Sub] = user.Id,
                [JwtRegisteredClaimNames.Email] = user.Email,
                [JwtClaimNames.Role] = user.Role.ToName(),
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(settings.SigningKeyBytes),
                SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(_handler.CreateToken(descriptor), expiresAt);
    }
}

/// <summary>Noms de claims courts partagés entre l'émission et la validation du token.</summary>
public static class JwtClaimNames
{
    public const string Role = "role";
}
