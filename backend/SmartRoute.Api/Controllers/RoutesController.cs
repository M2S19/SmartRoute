using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartRoute.Api.Auth;
using SmartRoute.Api.DTOs.Routes;
using SmartRoute.Api.Errors;
using SmartRoute.Api.Services;

namespace SmartRoute.Api.Controllers;

[ApiController]
[Route("api/routes")]
public class RoutesController : ControllerBase
{
    private readonly RouteOptimizationService _routeOptimizationService;
    private readonly RouteService _routeService;

    public RoutesController(
        RouteOptimizationService routeOptimizationService,
        RouteService routeService)
    {
        _routeOptimizationService = routeOptimizationService;
        _routeService = routeService;
    }

    /// <summary>Optimise l'ordre de passage d'une tournée (calcul seul, rien n'est enregistré).</summary>
    /// <response code="200">Ordre optimisé et distances.</response>
    /// <response code="401">Token absent, invalide ou expiré.</response>
    /// <response code="403">Utilisateur authentifié sans le rôle RESPONSABLE_LOGISTIQUE.</response>
    [HttpPost("optimize")]
    [Authorize(Policy = Policies.RequireManager)]
    public async Task<ActionResult<OptimizeRouteResponse>> Optimize(
        [FromBody] OptimizeRouteRequest request)
    {
        OptimizeRouteResponse result =
            await _routeOptimizationService.OptimizeAsync(request);

        return Ok(result);
    }

    /// <summary>Tournées affectées au livreur connecté (US3), de la plus récente à la plus ancienne.</summary>
    /// <response code="200">Tournées du livreur (liste vide s'il n'en a aucune).</response>
    /// <response code="401">Token absent, invalide ou expiré.</response>
    /// <response code="403">Utilisateur authentifié sans le rôle LIVREUR.</response>
    [HttpGet("me")]
    [Authorize(Policy = Policies.RequireDriver)]
    [ProducesResponseType<List<DriverRouteResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DriverRouteResponse>>> GetMyRoutes(CancellationToken cancellationToken)
    {
        return Ok(await _routeService.GetDriverRoutesAsync(CurrentUserId(), cancellationToken));
    }

    /// <summary>Marque un arrêt de sa tournée comme livré (US3) et renvoie la tournée à jour.</summary>
    /// <response code="200">Arrêt livré ; tournée à jour (statut, version serveur).</response>
    /// <response code="401">Token absent, invalide ou expiré.</response>
    /// <response code="403">Utilisateur authentifié sans le rôle LIVREUR.</response>
    /// <response code="404">Tournée introuvable ou d'un autre livreur, arrêt absent de la tournée (NOT_FOUND).</response>
    /// <response code="409">
    /// Tournée terminée (ROUTE_NOT_MODIFIABLE), arrêt déjà livré (STOP_ALREADY_DELIVERED),
    /// arrêt qui n'est pas le suivant (STOP_NOT_NEXT), modifications concurrentes (CONFLICT).
    /// </response>
    [HttpPatch("me/{routeId}/stops/{stopId}/deliver")]
    [Authorize(Policy = Policies.RequireDriver)]
    [ProducesResponseType<DriverRouteResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DriverRouteResponse>> DeliverStop(
        string routeId,
        string stopId,
        CancellationToken cancellationToken)
    {
        return Ok(await _routeService.DeliverStopAsync(CurrentUserId(), routeId, stopId, cancellationToken));
    }

    // Identifiant de l'utilisateur, uniquement depuis le JWT validé (claim « sub »).
    private string CurrentUserId() =>
        User.FindFirstValue(ClaimNames.Subject)
        ?? throw new ApiException(StatusCodes.Status401Unauthorized, ErrorCodes.Unauthorized, "Authentification requise.");
}
