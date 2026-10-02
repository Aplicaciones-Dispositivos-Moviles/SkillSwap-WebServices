using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using SkillSwap.Platform.Reputation.Application.QueryServices;
using SkillSwap.Platform.Reputation.Domain.Model;
using SkillSwap.Platform.Reputation.Domain.Model.Queries;
using SkillSwap.Platform.Reputation.Interfaces.Rest.Resources;
using SkillSwap.Platform.Reputation.Interfaces.Rest.Transform;
using SkillSwap.Platform.Shared.Interfaces.Rest;
using SkillSwap.Platform.Shared.Resources.Errors;
using Swashbuckle.AspNetCore.Annotations;

namespace SkillSwap.Platform.Reputation.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/verifier-reliabilities")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Available Verifier Reliability endpoints.")]
public class VerifierReliabilitiesController(
    IVerifierReliabilityQueryService reliabilityQueryService,
    IStringLocalizer<ErrorMessage> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpGet("{verifierUserId:int}")]
    [SwaggerOperation("Get Verifier Reliability by User Id",
        "Get the reliability of a verifier with the counters it is calculated from. It is recalculated by the " +
        "platform from the cases the verifier resolves; nobody rates a verifier directly. Only the verifier or a " +
        "Coordinator can read it.",
        OperationId = "GetVerifierReliabilityByUserId")]
    [SwaggerResponse(200, "The reliability of the verifier.", typeof(VerifierReliabilityResource))]
    [SwaggerResponse(403, "The reliability belongs to another user.")]
    [SwaggerResponse(404, "No reputation has been recorded for the verifier yet.")]
    public async Task<IActionResult> GetVerifierReliabilityByUserId(int verifierUserId,
        CancellationToken cancellationToken)
    {
        var actor = this.CurrentUser();
        if (actor.Id != verifierUserId && actor.Role != UserRole.Coordinator)
            return ReputationActionResultAssembler.ToErrorResult(
                this, problemDetailsFactory, errorLocalizer, ReputationError.NotReputationOwner);

        var reliability = await reliabilityQueryService.Handle(
            new GetVerifierReliabilityByUserIdQuery(verifierUserId), cancellationToken);
        return reliability is null
            ? ReputationActionResultAssembler.ToErrorResult(
                this, problemDetailsFactory, errorLocalizer, ReputationError.ReputationNotFound)
            : Ok(VerifierReliabilityResourceFromEntityAssembler.ToResourceFromEntity(reliability));
    }
}