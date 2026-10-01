using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using SkillSwap.Platform.LearningPathEngine.Application.CommandServices;
using SkillSwap.Platform.LearningPathEngine.Application.QueryServices;
using SkillSwap.Platform.LearningPathEngine.Domain.Model;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Commands;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Queries;
using SkillSwap.Platform.LearningPathEngine.Domain.Services;
using SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Resources;
using SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Transform;
using SkillSwap.Platform.Shared.Interfaces.Rest;
using SkillSwap.Platform.Shared.Resources.Errors;
using Swashbuckle.AspNetCore.Annotations;

namespace SkillSwap.Platform.LearningPathEngine.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/learning-paths")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Available Learning Path endpoints.")]
public class LearningPathsController(
    ILearningPathCommandService learningPathCommandService,
    ILearningPathQueryService learningPathQueryService,
    ISkillTaxonomy skillTaxonomy,
    IStringLocalizer<ErrorMessage> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Student))]
    [SwaggerOperation("Declare Goal",
        "Declare a career goal in free text. The goal is interpreted against the skill taxonomy, the skills " +
        "the student already demonstrated are discounted, and the learning path is built in prerequisite order. " +
        "The student is the authenticated user.",
        OperationId = "DeclareGoal")]
    [SwaggerResponse(201, "The learning path was created.", typeof(LearningPathResource))]
    [SwaggerResponse(400, "The goal is empty or longer than 500 characters.")]
    [SwaggerResponse(403, "Only students can declare a goal.")]
    [SwaggerResponse(409, "The student already has an active path, or already demonstrated every required skill.")]
    [SwaggerResponse(422, "The goal matches no skill of the taxonomy.")]
    public async Task<IActionResult> DeclareGoal(DeclareGoalResource resource, CancellationToken cancellationToken)
    {
        var command = DeclareGoalCommandFromResourceAssembler.ToCommandFromResource(resource, this.CurrentUserId());
        var result = await learningPathCommandService.Handle(command, cancellationToken);

        return LearningPathActionResultAssembler.ToActionResult(
            this,
            result,
            problemDetailsFactory,
            path => Created($"/api/v1/learning-paths/{path.StudentId}",
                LearningPathResourceFromEntityAssembler.ToResourceFromEntity(path, skillTaxonomy.NameOf)));
    }

    [HttpGet("{studentId:int}")]
    [SwaggerOperation("Get Learning Path by Student Id",
        "Get the latest learning path of a student with the state of each node. Only the student or a " +
        "Coordinator can read it. When the student reads their own path, the certificates uploaded since " +
        "it was created are linked to the matching nodes first (as supporting evidence only).",
        OperationId = "GetLearningPathByStudentId")]
    [SwaggerResponse(200, "The learning path.", typeof(LearningPathResource))]
    [SwaggerResponse(403, "The path belongs to another student.")]
    [SwaggerResponse(404, "The student has no learning path.")]
    public async Task<IActionResult> GetLearningPathByStudentId(int studentId, CancellationToken cancellationToken)
    {
        var actor = this.CurrentUser();
        if (actor.Id != studentId && actor.Role != UserRole.Coordinator)
            return LearningPathActionResultAssembler.ToErrorResult(
                this, problemDetailsFactory, errorLocalizer, LearningPathError.NotPathOwner);

        // A read by the owner also refreshes the certificate links; a Coordinator's read never writes.
        if (actor.Id == studentId)
        {
            var refreshed = await learningPathCommandService.Handle(new RefreshCertificateLinksCommand(studentId),
                cancellationToken);
            return LearningPathActionResultAssembler.ToActionResult(
                this,
                refreshed,
                problemDetailsFactory,
                path => Ok(LearningPathResourceFromEntityAssembler.ToResourceFromEntity(path,
                    skillTaxonomy.NameOf)));
        }

        var found = await learningPathQueryService.Handle(new GetLearningPathByStudentIdQuery(studentId),
            cancellationToken);
        return found is null
            ? LearningPathActionResultAssembler.ToErrorResult(
                this, problemDetailsFactory, errorLocalizer, LearningPathError.PathNotFound)
            : Ok(LearningPathResourceFromEntityAssembler.ToResourceFromEntity(found, skillTaxonomy.NameOf));
    }
}