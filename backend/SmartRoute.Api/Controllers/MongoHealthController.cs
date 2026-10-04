using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartRoute.Api.DTOs.Health;
using SmartRoute.Api.Errors;

namespace SmartRoute.Api.Controllers;

[ApiController]
[Route("api/mongo-health")]
public sealed class MongoHealthController(IMongoDatabase database, ILogger<MongoHealthController> logger) : ControllerBase
{
    private static readonly TimeSpan MongoPingTimeout = TimeSpan.FromSeconds(3);

    /// <summary>Vérifie que l'API communique avec MongoDB (ping). Sans authentification.</summary>
    /// <response code="200">MongoDB répond.</response>
    /// <response code="503">MongoDB injoignable (code SERVICE_UNAVAILABLE).</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<MongoHealthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<MongoHealthResponse>> Get(CancellationToken cancellationToken)
    {
        // Délai court : sans lui, le driver attendrait jusqu'à 30 s qu'un serveur MongoDB soit disponible.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(MongoPingTimeout);

        try
        {
            await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: timeout.Token);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Le détail technique reste dans les logs, pas dans la réponse.
            logger.LogWarning(exception, "Mongo health : MongoDB injoignable.");
            throw new ApiException(
                StatusCodes.Status503ServiceUnavailable,
                ErrorCodes.ServiceUnavailable,
                "La base de données est injoignable.");
        }

        return Ok(new MongoHealthResponse("ok", "ok"));
    }
}
