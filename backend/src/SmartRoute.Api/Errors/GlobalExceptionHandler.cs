using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartRoute.Api.Configuration;
using SmartRoute.Core.Exceptions;

namespace SmartRoute.Api.Errors;

/// <summary>
/// Convertit toute exception en ProblemDetails.
/// Erreurs métier : statut choisi ici (Core ne connaît pas HTTP). Le reste : 500 sans détail interne.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            InvalidCredentialsException e => DomainProblem(StatusCodes.Status401Unauthorized, "Identifiants invalides", e),
            _ => UnexpectedProblem(exception),
        };

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails DomainProblem(int status, string title, DomainException exception) => new()
    {
        Status = status,
        Title = title,
        Detail = exception.Message,
        Extensions = { [ProblemDetailsSetup.CodeExtension] = exception.Code },
    };

    private ProblemDetails UnexpectedProblem(Exception exception)
    {
        logger.LogError(exception, "Erreur non gérée.");

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Erreur interne",
        };

        // Détail technique uniquement en développement.
        if (environment.IsDevelopment())
        {
            problem.Extensions["exception"] = exception.ToString();
        }

        return problem;
    }
}
