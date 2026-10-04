using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartRoute.Api.DTOs.Health;

namespace SmartRoute.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>Vérifie que l'API ASP.NET Core répond (sans dépendance externe). Sans authentification.</summary>
    /// <remarks>Pour vérifier MongoDB : GET /api/mongo-health.</remarks>
    /// <response code="200">L'API répond.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> Get() => Ok(new HealthResponse("ok"));
}
