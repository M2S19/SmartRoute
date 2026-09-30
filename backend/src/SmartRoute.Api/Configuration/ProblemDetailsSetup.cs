using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using SmartRoute.Core.Exceptions;

namespace SmartRoute.Api.Configuration;

public static class ProblemDetailsSetup
{
    public const string CodeExtension = "code";

    /// <summary>
    /// Toutes les erreurs (validation, 401/403 du middleware JWT, exceptions, 404) sortent en
    /// ProblemDetails avec un « code » stable et un « detail » en français affichable par le front.
    /// </summary>
    public static IServiceCollection AddSmartRouteProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                var problem = context.ProblemDetails;
                problem.Instance ??= context.HttpContext.Request.Path;
                problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                if (problem.Extensions.ContainsKey(CodeExtension)) return;

                var (code, detail) = problem.Status switch
                {
                    StatusCodes.Status400BadRequest => (ErrorCodes.ValidationError, "Certains champs sont invalides."),
                    StatusCodes.Status401Unauthorized => (ErrorCodes.Unauthorized, "Authentification requise ou session expirée."),
                    StatusCodes.Status403Forbidden => (ErrorCodes.Forbidden, "Accès refusé."),
                    StatusCodes.Status404NotFound => (ErrorCodes.NotFound, "Ressource introuvable."),
                    >= StatusCodes.Status500InternalServerError => (ErrorCodes.InternalError, "Une erreur interne est survenue."),
                    _ => (null, null),
                };

                if (code is not null) problem.Extensions[CodeExtension] = code;
                problem.Detail ??= detail;
            };
        });

        return services;
    }

    /// <summary>400 automatique de [ApiController], passé par la même personnalisation.</summary>
    public static IActionResult CreateValidationResponse(ActionContext context)
    {
        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateValidationProblemDetails(
            context.HttpContext,
            context.ModelState,
            StatusCodes.Status400BadRequest,
            title: "Requête invalide",
            detail: "Certains champs sont invalides.");

        return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
    }
}
