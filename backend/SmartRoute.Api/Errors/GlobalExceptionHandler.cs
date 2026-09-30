using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SmartRoute.Api.Errors;

/// <summary>
/// Convertit toute exception en ProblemDetails :
/// <see cref="ApiException"/> → son statut et son code ; toute autre exception → 500 sans détail interne.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        if (exception is ApiException apiException)
        {
            problem = new ProblemDetails
            {
                Status = apiException.Status,
                Detail = apiException.Message,
                Extensions = { [ProblemDetailsConfig.CodeExtension] = apiException.Code },
            };
        }
        else
        {
            logger.LogError(exception, "Erreur non gérée.");
            problem = new ProblemDetails { Status = StatusCodes.Status500InternalServerError };

            // Détail technique uniquement en développement.
            if (environment.IsDevelopment()) problem.Extensions["exception"] = exception.ToString();
        }

        httpContext.Response.StatusCode = problem.Status.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
