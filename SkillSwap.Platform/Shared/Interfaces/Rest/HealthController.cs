using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using SkillSwap.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using SkillSwap.Platform.Shared.Interfaces.Rest.Resources;
using Swashbuckle.AspNetCore.Annotations;

namespace SkillSwap.Platform.Shared.Interfaces.Rest;

[ApiController]
[Route("health")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Service health endpoint.")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [SwaggerOperation("Check Health",
        "Check that the service is running. It needs no token and does not touch the database, so it is safe " +
        "for health checks and keep-alive pings.",
        OperationId = "CheckHealth")]
    [SwaggerResponse(200, "The service is running.", typeof(HealthResource))]
    public IActionResult CheckHealth()
    {
        return Ok(new HealthResource("Healthy", DateTime.UtcNow));
    }
}