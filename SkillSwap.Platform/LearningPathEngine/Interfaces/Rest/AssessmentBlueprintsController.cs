using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using SkillSwap.Platform.LearningPathEngine.Application.CommandServices;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Commands;
using SkillSwap.Platform.LearningPathEngine.Domain.Services;
using SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Resources;
using SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Transform;
using SkillSwap.Platform.Shared.Interfaces.Rest;
using Swashbuckle.AspNetCore.Annotations;

namespace SkillSwap.Platform.LearningPathEngine.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/path-nodes")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Available Assessment Blueprint endpoints.")]
public class AssessmentBlueprintsController(
    IAssessmentBlueprintCommandService assessmentBlueprintCommandService,
    ISkillTaxonomy skillTaxonomy,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpPost("{nodeId:int}/assessment-blueprint")]
    [Authorize(Roles = nameof(UserRole.Student))]
    [SwaggerOperation("Generate Assessment Blueprint",
        "Generate with AI the assessment of an available node. Only the owner of the path can request it. " +
        "The response contains the questions but never their correct answers. Requesting it again generates a " +
        "new assessment.",
        OperationId = "GenerateAssessmentBlueprint")]
    [SwaggerResponse(201, "The assessment was generated.", typeof(AssessmentBlueprintResource))]
    [SwaggerResponse(403, "The node belongs to another student's path.")]
    [SwaggerResponse(404, "The node was not found.")]
    [SwaggerResponse(409, "The node is locked (the response lists the pending prerequisites) or already completed.")]
    [SwaggerResponse(503, "The AI service is unavailable or returned an invalid assessment. The node is unchanged.")]
    public async Task<IActionResult> GenerateAssessmentBlueprint(int nodeId, CancellationToken cancellationToken)
    {
        var command = new GenerateAssessmentBlueprintCommand(nodeId, this.CurrentUserId());
        var result = await assessmentBlueprintCommandService.Handle(command, cancellationToken);

        return LearningPathActionResultAssembler.ToActionResult(
            this,
            result,
            problemDetailsFactory,
            blueprint => StatusCode(StatusCodes.Status201Created,
                AssessmentBlueprintResourceFromEntityAssembler.ToResourceFromEntity(blueprint,
                    skillTaxonomy.NameOf)));
    }
}