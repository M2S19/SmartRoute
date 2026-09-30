using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MongoDB.Driver;
using SmartRoute.Api.Auth;
using SmartRoute.Api.Errors;
using SmartRoute.Api.Models;
using SmartRoute.Api.Repositories;
using SmartRoute.Api.Services;
using SmartRoute.Api.Settings;

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuration (validée au démarrage) ----------

builder.Services.AddOptions<MongoSettings>()
    .Bind(builder.Configuration.GetSection(MongoSettings.SectionName))
    .Validate(s => !string.IsNullOrWhiteSpace(s.ConnectionString), "Mongo:ConnectionString est obligatoire.")
    .Validate(s => !string.IsNullOrWhiteSpace(s.DatabaseName), "Mongo:DatabaseName est obligatoire.")
    .ValidateOnStart();

builder.Services.AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
    .Validate(s => !string.IsNullOrWhiteSpace(s.Issuer) && !string.IsNullOrWhiteSpace(s.Audience),
        "Jwt:Issuer et Jwt:Audience sont obligatoires.")
    .Validate(s => s.LifetimeHours > 0, "Jwt:LifetimeHours doit être positif.")
    .Validate(s => s.SigningKeyBytes.Length >= JwtSettings.MinimumKeyBytes,
        $"Jwt:SigningKey absente ou trop courte (≥ {JwtSettings.MinimumKeyBytes} octets). " +
        "Définissez-la avec : dotnet user-secrets set \"Jwt:SigningKey\" \"<clé>\" --project SmartRoute.Api")
    .ValidateOnStart();

builder.Services.AddOptions<SeedSettings>().Bind(builder.Configuration.GetSection(SeedSettings.SectionName));

// ---------- MongoDB, repositories, services ----------

// Le client ne se connecte qu'à la première requête MongoDB.
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(sp.GetRequiredService<IOptions<MongoSettings>>().Value.ConnectionString));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(sp.GetRequiredService<IOptions<MongoSettings>>().Value.DatabaseName));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PasswordHasher<User>>();
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DatabaseSeeder>();

// ---------- MVC, erreurs ----------

builder.Services
    .AddControllers(options =>
        // Clés d'erreur de validation au nom JSON (« email ») plutôt qu'au nom C# (« Email »).
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = ProblemDetailsConfig.CreateValidationResponse)
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsConfig.Customize);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ---------- Authentification JWT et rôles ----------

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

// Configuré via les options pour lire JwtSettings validé (et la clé de test dans les tests).
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
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

builder.Services.AddAuthorizationBuilder()
    // Sécurisé par défaut : tout endpoint exige un token, sauf [AllowAnonymous].
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy(Policies.RequireManager, policy => policy.RequireRole(Roles.ResponsableLogistique))
    .AddPolicy(Policies.RequireDriver, policy => policy.RequireRole(Roles.Livreur));

// ---------- Swagger ----------

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartRoute API",
        Version = "v1",
        Description = "Pour tester une route protégée : POST /api/auth/login, puis « Authorize » avec le token.",
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Token renvoyé par POST /api/auth/login (sans le préfixe « Bearer »).",
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
    });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "SmartRoute.Api.xml"));
});

if (builder.Environment.IsDevelopment())
{
    // Front web (Expo) en développement uniquement ; l'app native n'est pas soumise à CORS.
    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
}

// ---------- Pipeline HTTP ----------

var app = builder.Build();

app.UseExceptionHandler();
// Donne un corps ProblemDetails aux réponses d'erreur vides (401/403 du middleware JWT, 404).
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.DocumentTitle = "SmartRoute API");
    app.UseCors();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await InitializeDatabaseAsync(app);

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    // Désactivé dans les tests (pas de MongoDB) via « Database:InitializeOnStartup ».
    if (!app.Configuration.GetValue("Database:InitializeOnStartup", defaultValue: true)) return;

    using var scope = app.Services.CreateScope();
    try
    {
        await scope.ServiceProvider.GetRequiredService<UserRepository>().EnsureIndexesAsync();

        if (app.Environment.IsDevelopment())
        {
            await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
        }
    }
    catch (TimeoutException exception)
    {
        app.Logger.LogCritical(exception,
            "MongoDB injoignable. Lancez-le avec « docker compose up -d » dans le dossier backend.");
        throw;
    }
}

// Exposé pour WebApplicationFactory<Program> dans les tests.
public partial class Program;
