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
[Route("api/v1/verifier-profiles")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Available Verifier Profile endpoints.")]
public class VerifierProfilesController(
    IVerifierProfileCommandService profileCommandService,
    IVerifierProfileQueryService profileQueryService,
    IStringLocalizer<ErrorMessage> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Student))]
    [SwaggerOperation("Create Verifier Profile",
        "Become a verifier of a skill the student has already completed. The first call creates the profile " +
        "(201); later calls add skills to it (200). The user is the authenticated student. Pending cases of the " +
        "skill are assigned right away.",
        OperationId = "CreateVerifierProfile")]
    [SwaggerResponse(201, "The verifier profile was created.", typeof(VerifierProfileResource))]
    [SwaggerResponse(200, "The skill was added to the existing profile.", typeof(VerifierProfileResource))]
    [SwaggerResponse(400, "The skill is empty.")]
    [SwaggerResponse(403, "The profile was revoked.")]
    [SwaggerResponse(409, "The student has not completed the skill, or is already a verifier of it.")]
    public async Task<IActionResult> CreateVerifierProfile(CreateVerifierProfileResource resource,
        CancellationToken cancellationToken)
    {
        var command = CreateVerifierProfileCommandFromResourceAssembler.ToCommandFromResource(resource,
            this.CurrentUserId());
        var result = await profileCommandService.Handle(command, cancellationToken);

        return AssessmentPeerReviewActionResultAssembler.ToActionResult(
            this,
            result,
            problemDetailsFactory,
            profile =>
            {
                var body = VerifierProfileResourceFromEntityAssembler.ToResourceFromEntity(profile);
                // A profile that has a single skill was just created: adding a skill always leaves at least two.
                return profile.SkillTags.Count == 1
                    ? Created("/api/v1/verifier-profiles/me", body)
                    : Ok(body);
            });
    }

    [HttpGet("me")]
    [Authorize(Roles = nameof(UserRole.Student))]
    [SwaggerOperation("Get My Verifier Profile",
        "Get the verifier profile of the authenticated student.",
        OperationId = "GetMyVerifierProfile")]
    [SwaggerResponse(200, "The verifier profile.", typeof(VerifierProfileResource))]
    [SwaggerResponse(404, "The student is not a verifier yet.")]
    public async Task<IActionResult> GetMyVerifierProfile(CancellationToken cancellationToken)
    {
        var profile = await profileQueryService.Handle(new GetVerifierProfileByUserIdQuery(this.CurrentUserId()),
            cancellationToken);
        return profile is null
            ? AssessmentPeerReviewActionResultAssembler.ToErrorResult(
                this, problemDetailsFactory, errorLocalizer, AssessmentPeerReviewError.VerifierProfileNotFound)
            : Ok(VerifierProfileResourceFromEntityAssembler.ToResourceFromEntity(profile));
    }

    [HttpPatch("me/availability")]
    [Authorize(Roles = nameof(UserRole.Student))]
    [SwaggerOperation("Update My Verifier Availability",
        "Switch on or off the availability of the authenticated verifier. Turning it on assigns the pending " +
        "cases of their skills; turning it off keeps the cases already assigned. The availability is required.",
        OperationId = "UpdateMyVerifierAvailability")]
    [SwaggerResponse(200, "The updated verifier profile.", typeof(VerifierProfileResource))]
    [SwaggerResponse(400, "The availability was not sent.")]
    [SwaggerResponse(403, "The caller is not an enabled verifier.")]
    public async Task<IActionResult> UpdateMyVerifierAvailability(VerifierAvailabilityResource resource,
        CancellationToken cancellationToken)
    {
        if (resource.Available is null)
            return AssessmentPeerReviewActionResultAssembler.ToErrorResult(
                this, problemDetailsFactory, errorLocalizer, AssessmentPeerReviewError.InvalidAvailability);

        var command = UpdateVerifierAvailabilityCommandFromResourceAssembler.ToCommandFromResource(resource,
            this.CurrentUserId());
        var result = await profileCommandService.Handle(command, cancellationToken);

        return AssessmentPeerReviewActionResultAssembler.ToActionResult(
            this,
            result,
            problemDetailsFactory,
            profile => Ok(VerifierProfileResourceFromEntityAssembler.ToResourceFromEntity(profile)));
    }
}