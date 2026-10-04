using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartRoute.Api.Settings;

namespace SmartRoute.Api.Auth;

/// <summary>Configuration de l'authentification JWT et des politiques de rôles, appelée par Program.cs.</summary>
public static class AuthSetup
{
    public static IServiceCollection AddSmartRouteAuth(this IServiceCollection services, IConfiguration configuration)
    {
        // Paramètres JWT validés au démarrage : l'API refuse de démarrer sans clé valide.
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .Validate(s => !string.IsNullOrWhiteSpace(s.Issuer) && !string.IsNullOrWhiteSpace(s.Audience),
                "Jwt:Issuer et Jwt:Audience sont obligatoires.")
            .Validate(s => s.LifetimeHours > 0, "Jwt:LifetimeHours doit être positif.")
            .Validate(s => s.SigningKeyBytes.Length >= JwtSettings.MinimumKeyBytes,
                $"Jwt:SigningKey absente ou trop courte (≥ {JwtSettings.MinimumKeyBytes} octets). " +
                "Définissez-la avec : dotnet user-secrets set \"Jwt:SigningKey\" \"<clé>\" --project SmartRoute.Api")
            .ValidateOnStart();

        services.AddSingleton<PasswordHasher>();
        services.AddSingleton<JwtTokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configuré via les options pour lire JwtSettings validé (et la clé de test dans les tests).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                // Garde les claims courts (« sub », « role ») au lieu des URI Microsoft.
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
                    NameClaimType = ClaimNames.Subject,
                    RoleClaimType = ClaimNames.Role,
                };
            });

        services.AddAuthorizationBuilder()
            // Sécurisé par défaut : tout endpoint exige un token, sauf [AllowAnonymous].
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.RequireManager, policy => policy.RequireRole(Roles.ResponsableLogistique))
            .AddPolicy(Policies.RequireDriver, policy => policy.RequireRole(Roles.Livreur));

        return services;
    }
}
