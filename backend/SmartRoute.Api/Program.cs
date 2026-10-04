using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using MongoDB.Driver;
using SmartRoute.Api.Auth;
using SmartRoute.Api.Errors;
using SmartRoute.Api.Repositories;
using SmartRoute.Api.Services;
using SmartRoute.Api.Settings;

var builder = WebApplication.CreateBuilder(args);

// ---------- MongoDB (configuration validée au démarrage) ----------

builder.Services.AddOptions<MongoDbSettings>()
    .Bind(builder.Configuration.GetSection(MongoDbSettings.SectionName))
    .Validate(s => !string.IsNullOrWhiteSpace(s.ConnectionString), "MongoDb:ConnectionString est obligatoire.")
    .Validate(s => !string.IsNullOrWhiteSpace(s.DatabaseName), "MongoDb:DatabaseName est obligatoire.")
    .ValidateOnStart();

// Le client ne se connecte qu'à la première requête MongoDB.
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(sp.GetRequiredService<IOptions<MongoDbSettings>>().Value.ConnectionString));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(sp.GetRequiredService<IOptions<MongoDbSettings>>().Value.DatabaseName));

// ---------- Repositories, services, authentification ----------

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddOptions<SeedSettings>().Bind(builder.Configuration.GetSection(SeedSettings.SectionName));
builder.Services.AddScoped<DatabaseSeeder>();

// JWT (clé vérifiée au démarrage), PasswordHasher, JwtTokenService, politiques de rôles : voir Auth/AuthSetup.cs.
builder.Services.AddSmartRouteAuth(builder.Configuration);

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
            "MongoDB injoignable. En DEV, depuis la racine du dépôt : " +
            "« docker compose --env-file .env.dev -f docker-compose.yml -f docker-compose.dev.yml up -d mongo ».");
        throw;
    }
}

// Exposé pour WebApplicationFactory<Program> dans les tests.
public partial class Program;
