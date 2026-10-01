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
[Route("api/v1/verification-cases")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Available Verification Case endpoints.")]
public class VerificationCasesController(
    IVerificationCaseCommandService caseCommandService,
    IVerificationCaseQueryService caseQueryService,
    IStringLocalizer<ErrorMessage> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Student))]
    [SwaggerOperation("Get My Assigned Verification Cases",
        "List the verification cases assigned to the authenticated verifier, newest first. The verifier is " +
        "always the authenticated user; the list is empty for a student who is not a verifier.",
        OperationId = "GetMyAssignedVerificationCases")]
    [SwaggerResponse(200, "The cases assigned to the caller.", typeof(IEnumerable<VerificationCaseResource>))]
    public async Task<IActionResult> GetMyAssignedVerificationCases(CancellationToken cancellationToken)
    {
        var cases = await caseQueryService.Handle(new GetVerificationCasesByVerifierQuery(this.CurrentUserId()),
            cancellationToken);
        return Ok(cases.Select(VerificationCaseResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{caseId:int}")]
    [SwaggerOperation("Get Verification Case by Id",
        "Get a verification case with its attempt and the questions the student answered incorrectly, with " +
        "the option they chose and never the correct answer. Only the student, the assigned verifier or a " +
        "Coordinator can read it.",
        OperationId = "GetVerificationCaseById")]
    [SwaggerResponse(200, "The case.", typeof(VerificationCaseDetailResource))]
    [SwaggerResponse(403, "The case belongs to another student and is not assigned to the caller.")]
    [SwaggerResponse(404, "The case was not found.")]
    public async Task<IActionResult> GetVerificationCaseById(int caseId, CancellationToken cancellationToken)
    {
        var detail = await caseQueryService.Handle(new GetVerificationCaseDetailQuery(caseId), cancellationToken);
        if (detail is null)
            return AssessmentPeerReviewActionResultAssembler.ToErrorResult(
                this, problemDetailsFactory, errorLocalizer, AssessmentPeerReviewError.CaseNotFound);

        var actor = this.CurrentUser();
        var canRead = actor.Role == UserRole.Coordinator
                      || detail.Case.StudentId == actor.Id
                      || detail.Case.IsAssignedTo(actor.Id);
        if (!canRead)
            return AssessmentPeerReviewActionResultAssembler.ToErrorResult(
                this, problemDetailsFactory, errorLocalizer, AssessmentPeerReviewError.NotCaseOwner);

        return Ok(VerificationCaseDetailResourceFromDetailAssembler.ToResourceFromDetail(detail));
    }

    [HttpPut("{caseId:int}/evidence")]
    [Authorize(Roles = nameof(UserRole.Student))]
    [SwaggerOperation("Attach Case Evidence",
        "Attach the link to the student's repository or portfolio to an unresolved case. A new link replaces " +
        "the previous one. Only the student who owns the case can do it.",
        OperationId = "AttachCaseEvidence")]
    [SwaggerResponse(200, "The case with its evidence.", typeof(VerificationCaseResource))]
    [SwaggerResponse(400, "The link is not a valid http or https URL of up to 500 characters.")]
    [SwaggerResponse(403, "The case belongs to another student.")]
    [SwaggerResponse(404, "The case was not found.")]
    [SwaggerResponse(409, "The case is already resolved.")]
    public async Task<IActionResult> AttachCaseEvidence(int caseId, AttachEvidenceResource resource,
        CancellationToken cancellationToken)
    {
        var command = AttachCaseEvidenceCommandFromResourceAssembler.ToCommandFromResource(caseId, resource,
            this.CurrentUserId());
        var result = await caseCommandService.Handle(command, cancellationToken);

        return AssessmentPeerReviewActionResultAssembler.ToActionResult(
            this,
            result,
            problemDetailsFactory,
            verificationCase => Ok(VerificationCaseResourceFromEntityAssembler.ToResourceFromEntity(verificationCase)));
    }

    [HttpPatch("{caseId:int}/decision")]
    [Authorize(Roles = nameof(UserRole.Student))]
    [SwaggerOperation("Resolve Verification Case",
        "Record the decision of the assigned verifier (Approved or Rejected) with the notes of the rubric. " +
        "Approving completes the node of the student; rejecting leaves it available, so the student can " +
        "request a new assessment. Only the assigned verifier can do it.",
        OperationId = "ResolveVerificationCase")]
    [SwaggerResponse(200, "The resolved case.", typeof(VerificationCaseResource))]
    [SwaggerResponse(400, "The decision is not valid or the rubric notes are empty or longer than 2000 characters.")]
    [SwaggerResponse(403, "The case is not assigned to the caller, or the caller is not an enabled verifier.")]
    [SwaggerResponse(404, "The case was not found.")]
    [SwaggerResponse(409, "The case is already resolved, or its node can no longer be completed.")]
    public async Task<IActionResult> ResolveVerificationCase(int caseId, ResolveCaseResource resource,
        CancellationToken cancellationToken)
    {
        var command = ResolveVerificationCaseCommandFromResourceAssembler.ToCommandFromResource(caseId, resource,
            this.CurrentUserId());
        var result = await caseCommandService.Handle(command, cancellationToken);

        return AssessmentPeerReviewActionResultAssembler.ToActionResult(
            this,
            result,
            problemDetailsFactory,
            verificationCase => Ok(VerificationCaseResourceFromEntityAssembler.ToResourceFromEntity(verificationCase)));
    }
}