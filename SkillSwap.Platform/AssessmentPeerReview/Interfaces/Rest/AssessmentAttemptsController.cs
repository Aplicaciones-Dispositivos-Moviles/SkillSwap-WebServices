using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using SkillSwap.Platform.AssessmentPeerReview.Application.CommandServices;
using SkillSwap.Platform.AssessmentPeerReview.Application.QueryServices;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Queries;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Transform;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using SkillSwap.Platform.Shared.Interfaces.Rest;
using SkillSwap.Platform.Shared.Resources.Errors;
using Swashbuckle.AspNetCore.Annotations;

namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest;

[Authorize]
[ApiController]
[Route("api/v1/assessment-attempts")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Available Assessment Attempt endpoints.")]
public class AssessmentAttemptsController(
    IAssessmentAttemptCommandService attemptCommandService,
    IAssessmentAttemptQueryService attemptQueryService,
    IStringLocalizer<ErrorMessage> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Student))]
    [SwaggerOperation("Submit Assessment Attempt",
        "Submit the answers of an assessment. The attempt is graded on the server: 4 or more correct answers " +
        "out of 5 complete the node. Otherwise a verification case is opened and assigned to an available " +
        "verifier, and the response reports its id and status. Only the owner of the path can answer, only " +
        "the latest assessment of the node can be answered, and each assessment accepts a single attempt. " +
        "While the node has an unresolved case no new attempt is accepted.",
        OperationId = "SubmitAssessmentAttempt")]
    [SwaggerResponse(201, "The attempt was graded.", typeof(AssessmentAttemptResource))]
    [SwaggerResponse(400, "There is not exactly one answer per question, each between 0 and 3.")]
    [SwaggerResponse(403, "The assessment belongs to another student.")]
    [SwaggerResponse(404, "The assessment was not found.")]
    [SwaggerResponse(409, "The assessment is outdated or already answered, the node is not available, " +
                          "or it has a verification case in progress.")]
    public async Task<IActionResult> SubmitAssessmentAttempt(SubmitAssessmentAttemptResource resource,
        CancellationToken cancellationToken)
    {
        var command =
            SubmitAssessmentAttemptCommandFromResourceAssembler.ToCommandFromResource(resource,
                this.CurrentUserId());
        var result = await attemptCommandService.Handle(command, cancellationToken);

        return AssessmentPeerReviewActionResultAssembler.ToActionResult(
            this,
            result,
            problemDetailsFactory,
            outcome => Created($"/api/v1/assessment-attempts/{outcome.Attempt.Id}",
                AssessmentAttemptResourceFromEntityAssembler.ToResourceFromEntity(outcome.Attempt,
                    outcome.VerificationCase)));
    }

    [HttpGet("{attemptId:int}")]
    [SwaggerOperation("Get Assessment Attempt by Id",
        "Get an assessment attempt with its score. Only the student or a Coordinator can read it. The " +
        "verification case opened by a failed attempt is reported in the response to the submission and by " +
        "the verification case endpoints; here it is not filled in.",
        OperationId = "GetAssessmentAttemptById")]
    [SwaggerResponse(200, "The attempt.", typeof(AssessmentAttemptResource))]
    [SwaggerResponse(403, "The attempt belongs to another student.")]
    [SwaggerResponse(404, "The attempt was not found.")]
    public async Task<IActionResult> GetAssessmentAttemptById(int attemptId, CancellationToken cancellationToken)
    {
        var attempt = await attemptQueryService.Handle(new GetAssessmentAttemptByIdQuery(attemptId),
            cancellationToken);
        if (attempt is null)
            return AssessmentPeerReviewActionResultAssembler.ToErrorResult(
                this, problemDetailsFactory, errorLocalizer, AssessmentPeerReviewError.AttemptNotFound);

        var actor = this.CurrentUser();
        if (attempt.StudentId != actor.Id && actor.Role != UserRole.Coordinator)
            return AssessmentPeerReviewActionResultAssembler.ToErrorResult(
                this, problemDetailsFactory, errorLocalizer, AssessmentPeerReviewError.NotAttemptOwner);

        return Ok(AssessmentAttemptResourceFromEntityAssembler.ToResourceFromEntity(attempt));
    }
}