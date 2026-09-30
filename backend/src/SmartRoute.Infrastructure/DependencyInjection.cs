using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SmartRoute.Core.Interfaces;
using SmartRoute.Infrastructure.Mongo;
using SmartRoute.Infrastructure.Repositories;
using SmartRoute.Infrastructure.Security;
using SmartRoute.Infrastructure.Seeding;

namespace SmartRoute.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        MongoMappings.Register();

        services.AddOptions<MongoSettings>()
            .Bind(configuration.GetSection(MongoSettings.SectionName))
            .Validate(s => !string.IsNullOrWhiteSpace(s.ConnectionString), "Mongo:ConnectionString est obligatoire.")
            .Validate(s => !string.IsNullOrWhiteSpace(s.DatabaseName), "Mongo:DatabaseName est obligatoire.")
            .ValidateOnStart();

        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .Validate(s => !string.IsNullOrWhiteSpace(s.Issuer), "Jwt:Issuer est obligatoire.")
            .Validate(s => !string.IsNullOrWhiteSpace(s.Audience), "Jwt:Audience est obligatoire.")
            .Validate(s => s.LifetimeHours > 0, "Jwt:LifetimeHours doit être positif.")
            .Validate(
                s => s.HasValidSigningKey,
                $"Jwt:SigningKey absente ou trop courte (≥ {JwtSettings.MinimumKeyBytes} octets). " +
                "Définissez-la avec : dotnet user-secrets set \"Jwt:SigningKey\" \"<clé>\" --project src/SmartRoute.Api")
            .ValidateOnStart();

        services.AddOptions<SeedSettings>().Bind(configuration.GetSection(SeedSettings.SectionName));

        // Le client ne se connecte qu'à la première requête : aucune connexion tant que MongoDB n'est pas utilisé.
        services.AddSingleton<IMongoClient>(sp =>
            new MongoClient(sp.GetRequiredService<IOptions<MongoSettings>>().Value.ConnectionString));
        services.AddSingleton(sp =>
            sp.GetRequiredService<IMongoClient>()
                .GetDatabase(sp.GetRequiredService<IOptions<MongoSettings>>().Value.DatabaseName));

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
