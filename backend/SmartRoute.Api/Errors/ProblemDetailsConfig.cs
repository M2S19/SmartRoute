using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace SmartRoute.Api.Errors;

/// <summary>
/// Toutes les erreurs (validation, 401/403 du middleware JWT, exceptions, 404) sortent en ProblemDetails
/// avec un « code » stable et un « detail » en français, affichable tel quel par le front.
/// </summary>
public static class ProblemDetailsConfig
{
    public const string CodeExtension = "code";

    /// <summary>Complète chaque ProblemDetails : traceId, instance, et code/detail par défaut selon le statut.</summary>
    public static void Customize(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        problem.Instance ??= context.HttpContext.Request.Path;
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        var (code, title, detail) = problem.Status switch
        {
            StatusCodes.Status400BadRequest => (ErrorCodes.ValidationError, "Requête invalide", "Certains champs sont invalides."),
            StatusCodes.Status401Unauthorized => (ErrorCodes.Unauthorized, "Non authentifié", "Authentification requise ou session expirée."),
            StatusCodes.Status403Forbidden => (ErrorCodes.Forbidden, "Accès refusé", "Accès refusé."),
            StatusCodes.Status404NotFound => (ErrorCodes.NotFound, "Introuvable", "Ressource introuvable."),
            StatusCodes.Status409Conflict => (ErrorCodes.Conflict, "Conflit", "L'opération est en conflit avec l'état actuel des données."),
            StatusCodes.Status422UnprocessableEntity => (ErrorCodes.UnprocessableEntity, "Requête non traitable", "La requête ne peut pas être traitée."),
            StatusCodes.Status503ServiceUnavailable => (ErrorCodes.ServiceUnavailable, "Service indisponible", "Service temporairement indisponible."),
            >= StatusCodes.Status500InternalServerError => (ErrorCodes.InternalError, "Erreur interne", "Une erreur interne est survenue."),
            _ => ((string?)null, (string?)null, (string?)null),
        };

        // Les valeurs déjà posées (ex. code INVALID_CREDENTIALS d'une ApiException) sont conservées.
        if (code is not null) problem.Extensions.TryAdd(CodeExtension, code);
        problem.Detail ??= detail;
        // Remplace les titres anglais par défaut d'ASP.NET Core.
        if (title is not null) problem.Title = title;
    }

    /// <summary>Réponse 400 automatique de [ApiController], passée par la même personnalisation.</summary>
    public static IActionResult CreateValidationResponse(ActionContext context)
    {
        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateValidationProblemDetails(context.HttpContext, context.ModelState, StatusCodes.Status400BadRequest);

        return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
    }
}
