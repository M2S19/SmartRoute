using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartRoute.Api.Auth;
using SmartRoute.Api.DTOs.Deliveries;
using SmartRoute.Api.Services;

namespace SmartRoute.Api.Controllers;

/// <summary>Affectation d'une nouvelle livraison (US2) : réservée au responsable logistique.</summary>
[ApiController]
[Route("api/deliveries")]
[Authorize(Policy = Policies.RequireManager)]
public class DeliveriesController : ControllerBase
{
    private readonly DeliveryAssignmentService _deliveryAssignmentService;
    private readonly TimeProvider _timeProvider;

    public DeliveriesController(
        DeliveryAssignmentService deliveryAssignmentService,
        TimeProvider timeProvider)
    {
        _deliveryAssignmentService = deliveryAssignmentService;
        _timeProvider = timeProvider;
    }

    /// <summary>Propose la tournée et la position d'insertion au plus faible surcoût (rien n'est enregistré).</summary>
    /// <response code="200">Proposition.</response>
    /// <response code="400">Adresse manquante (VALIDATION_ERROR).</response>
    /// <response code="401">Token absent, invalide ou expiré.</response>
    /// <response code="403">Utilisateur sans le rôle RESPONSABLE_LOGISTIQUE.</response>
    /// <response code="422">Aucune tournée du jour ne peut recevoir la livraison (NO_ELIGIBLE_ROUTE).</response>
    [HttpPost("assign-best-driver")]
    public async Task<ActionResult<AssignmentProposalResponse>> AssignBestDriver(
        [FromBody] AssignBestDriverRequest request,
        CancellationToken cancellationToken)
    {
        AssignmentProposalResponse result =
            await _deliveryAssignmentService.FindBestRouteAsync(
                request.Address.Label,
                Today(),
                cancellationToken
            );

        return Ok(result);
    }

    /// <summary>Confirme une proposition : la livraison est enregistrée comme nouvel arrêt de la tournée.</summary>
    /// <response code="201">Livraison enregistrée dans la tournée.</response>
    /// <response code="400">Requête invalide (VALIDATION_ERROR).</response>
    /// <response code="401">Token absent, invalide ou expiré.</response>
    /// <response code="403">Utilisateur sans le rôle RESPONSABLE_LOGISTIQUE.</response>
    /// <response code="404">Tournée introuvable (NOT_FOUND).</response>
    /// <response code="409">Tournée non éligible (ROUTE_NOT_ELIGIBLE) ou modifiée depuis la proposition (PROPOSAL_OUTDATED).</response>
    [HttpPost("confirm-assignment")]
    [ProducesResponseType<ConfirmAssignmentResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ConfirmAssignmentResponse>> ConfirmAssignment(
        [FromBody] ConfirmAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        ConfirmAssignmentResponse result =
            await _deliveryAssignmentService.ConfirmAssignmentAsync(request, Today(), cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    // Jour de la tournée, en heure locale du serveur (même règle qu'avant : DateTime.Now).
    private string Today() =>
        _timeProvider.GetLocalNow().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
