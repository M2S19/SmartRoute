using Microsoft.AspNetCore.Mvc;

namespace SmartRoute.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "SmartRoute API is running"
        });
    }
}