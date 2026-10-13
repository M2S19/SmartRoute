using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartRoute.Api.DTOs.Routes;
using SmartRoute.Api.Services;

namespace SmartRoute.Api.Controllers;

[ApiController]
[Route("api/routes")]
public class RoutesController : ControllerBase
{
    private readonly RouteOptimizationService _routeOptimizationService;

    public RoutesController(
        RouteOptimizationService routeOptimizationService)
    {
        _routeOptimizationService = routeOptimizationService;
    }

    [HttpPost("optimize")]
    [AllowAnonymous] // Temporaire pour les tests Swagger
    public async Task<ActionResult<OptimizeRouteResponse>> Optimize(
        [FromBody] OptimizeRouteRequest request)
    {
        OptimizeRouteResponse result =
            await _routeOptimizationService.OptimizeAsync(request);

        return Ok(result);
    }
}