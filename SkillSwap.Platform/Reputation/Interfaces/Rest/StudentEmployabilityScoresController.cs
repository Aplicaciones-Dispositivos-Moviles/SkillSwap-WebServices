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
[Route("api/v1/student-employability-scores")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Available Student Employability endpoints.")]
public class StudentEmployabilityScoresController(
    IStudentEmployabilityQueryService employabilityQueryService,
    IStringLocalizer<ErrorMessage> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpGet("{studentId:int}")]
    [SwaggerOperation("Get Student Employability by Student Id",
        "Get the employability a student demonstrated: 10 points for each skill they certified, by automatic " +
        "approval or by the decision of a verifier, up to 100. Only the student or a Coordinator can read it.",
        OperationId = "GetStudentEmployabilityByStudentId")]
    [SwaggerResponse(200, "The employability of the student.", typeof(StudentEmployabilityResource))]
    [SwaggerResponse(403, "The employability belongs to another student.")]
    [SwaggerResponse(404, "The student has not certified any skill yet.")]
    public async Task<IActionResult> GetStudentEmployabilityByStudentId(int studentId,
        CancellationToken cancellationToken)
    {
        var actor = this.CurrentUser();
        if (actor.Id != studentId && actor.Role != UserRole.Coordinator)
            return ReputationActionResultAssembler.ToErrorResult(
                this, problemDetailsFactory, errorLocalizer, ReputationError.NotReputationOwner);

        var employability = await employabilityQueryService.Handle(
            new GetStudentEmployabilityByStudentIdQuery(studentId), cancellationToken);
        return employability is null
            ? ReputationActionResultAssembler.ToErrorResult(
                this, problemDetailsFactory, errorLocalizer, ReputationError.ReputationNotFound)
            : Ok(StudentEmployabilityResourceFromEntityAssembler.ToResourceFromEntity(employability));
    }
}