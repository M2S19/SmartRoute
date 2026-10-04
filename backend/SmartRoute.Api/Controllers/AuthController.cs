using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartRoute.Api.DTOs.Auth;
using SmartRoute.Api.Services;

namespace SmartRoute.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    /// <summary>Connexion : renvoie un JWT (12 h) et l'utilisateur.</summary>
    /// <response code="200">Connexion réussie.</response>
    /// <response code="400">Email ou mot de passe manquant, email mal formé (code VALIDATION_ERROR).</response>
    /// <response code="401">Identifiants refusés (code INVALID_CREDENTIALS).</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        // Email et Password non nuls : garantis par [Required] (sinon 400 automatique avant d'arriver ici).
        return Ok(await authService.LoginAsync(request.Email!, request.Password!, cancellationToken));
    }
}
