using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SmartRoute.Api.Configuration;

public static class SwaggerSetup
{
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddSmartRouteSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SmartRoute API",
                Version = "v1",
                Description = "API de l'application mobile SmartRoute. " +
                              "Pour tester une route protégée : POST /api/auth/login, puis « Authorize » avec le token.",
            });

            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Token renvoyé par POST /api/auth/login (sans le préfixe « Bearer »).",
            });

            options.OperationFilter<BearerRequirementOperationFilter>();

            var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{typeof(SwaggerSetup).Assembly.GetName().Name}.xml");
            if (File.Exists(xmlFile)) options.IncludeXmlComments(xmlFile);
        });

        return services;
    }

    /// <summary>Cadenas Swagger sur les seuls endpoints protégés (tous sauf [AllowAnonymous]).</summary>
    private sealed class BearerRequirementOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var isAnonymous = context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();
            if (isAnonymous) return;

            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerScheme, context.Document)] = [],
            });
        }
    }
}
