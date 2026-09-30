using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SmartRoute.Api.Authorization;
using SmartRoute.Core.Enums;
using SmartRoute.Infrastructure.Security;

namespace SmartRoute.Api.Configuration;

public static class AuthSetup
{
    public static IServiceCollection AddSmartRouteAuth(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configuré via les options (et non à la construction) pour lire JwtSettings déjà validé,
        // y compris quand les tests remplacent la clé.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                // Garde les noms de claims courts (« sub », « role ») au lieu des URI Microsoft.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(jwt.SigningKeyBytes),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = JwtClaimNames.Role,
                };
            });

        services.AddAuthorizationBuilder()
            // Sécurisé par défaut : tout endpoint exige un token, sauf [AllowAnonymous].
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.RequireManager, policy => policy.RequireRole(UserRoleNames.ResponsableLogistique))
            .AddPolicy(Policies.RequireDriver, policy => policy.RequireRole(UserRoleNames.Livreur));

        return services;
    }
}
