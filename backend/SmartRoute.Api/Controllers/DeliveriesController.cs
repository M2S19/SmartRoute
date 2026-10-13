using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartRoute.Api.DTOs.Deliveries;
using SmartRoute.Api.Services;

namespace SmartRoute.Api.Controllers;

[ApiController]
[Route("api/deliveries")]
public class DeliveriesController : ControllerBase
{
    private readonly DeliveryAssignmentService _deliveryAssignmentService;

    public DeliveriesController(
        DeliveryAssignmentService deliveryAssignmentService)
    {
        _deliveryAssignmentService = deliveryAssignmentService;
    }

    [HttpPost("assign-best-driver")]
    [AllowAnonymous]
    public async Task<ActionResult<AssignmentProposalResponse>> AssignBestDriver(
        [FromBody] AssignBestDriverRequest request,
        CancellationToken cancellationToken)
    {
        string date = DateTime.Now.ToString("yyyy-MM-dd");

        AssignmentProposalResponse result =
            await _deliveryAssignmentService.FindBestRouteAsync(
                request.Address.Label,
                date,
                cancellationToken
            );

        return Ok(result);
    }
}