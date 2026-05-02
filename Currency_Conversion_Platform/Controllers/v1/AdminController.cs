using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConversionPlatform.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminController(IWebHostEnvironment env) : ControllerBase
{
    [HttpGet("info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetInfo() =>
        Ok(new
        {
            environment = env.EnvironmentName,
            version = "1.0",
            timestamp = DateTime.UtcNow
        });
}
