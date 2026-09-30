using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using SmartRoute.Api.Configuration;
using SmartRoute.Api.Errors;
using SmartRoute.Core.Services;
using SmartRoute.Infrastructure;
using SmartRoute.Infrastructure.Mongo;
using SmartRoute.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<AuthService>();

builder.Services
    .AddControllers(options =>
    {
        // Clés d'erreur de validation au nom JSON (« email ») plutôt qu'au nom C# (« Email »).
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
    })
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = ProblemDetailsSetup.CreateValidationResponse)
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

builder.Services.AddSmartRouteProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddSmartRouteAuth();
builder.Services.AddSmartRouteSwagger();

if (builder.Environment.IsDevelopment())
{
    // Web (Expo) en développement uniquement ; l'app native n'est pas concernée par CORS.
    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
}

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
    // Désactivable (tests sans MongoDB) via « Database:InitializeOnStartup ».
    if (!app.Configuration.GetValue("Database:InitializeOnStartup", defaultValue: true)) return;

    using var scope = app.Services.CreateScope();
    try
    {
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().EnsureIndexesAsync();

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
