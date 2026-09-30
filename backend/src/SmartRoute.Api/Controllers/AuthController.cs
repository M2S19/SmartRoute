using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartRoute.Api.Contracts.Auth;
using SmartRoute.Core.Services;

namespace SmartRoute.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    /// <summary>Connexion : renvoie un JWT et l'utilisateur.</summary>
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
        // Email et Password non nuls : garantis par la validation [Required] (sinon 400 automatique).
        var result = await authService.LoginAsync(request.Email!, request.Password!, cancellationToken);

        return Ok(new LoginResponse(result.AccessToken.Token, result.AccessToken.ExpiresAtUtc, UserDto.From(result.User)));
    }
}
