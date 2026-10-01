using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Interfaces.Rest.ProblemDetails;
using SkillSwap.Platform.Shared.Resources.Errors;

namespace SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Transform;

/// <summary>
///     Maps application results and domain errors to HTTP responses
/// </summary>
public static class AssessmentPeerReviewActionResultAssembler
{
    private static int ToStatusCodeFromError(AssessmentPeerReviewError error)
    {
        return error switch
        {
            AssessmentPeerReviewError.InvalidAnswers => StatusCodes.Status400BadRequest,
            AssessmentPeerReviewError.InvalidEvidenceUrl => StatusCodes.Status400BadRequest,
            AssessmentPeerReviewError.InvalidSkillTag => StatusCodes.Status400BadRequest,
            AssessmentPeerReviewError.InvalidDecision => StatusCodes.Status400BadRequest,
            AssessmentPeerReviewError.InvalidAvailability => StatusCodes.Status400BadRequest,
            AssessmentPeerReviewError.RubricNotesRequired => StatusCodes.Status400BadRequest,
            AssessmentPeerReviewError.RubricNotesTooLong => StatusCodes.Status400BadRequest,
            AssessmentPeerReviewError.NotBlueprintOwner => StatusCodes.Status403Forbidden,
            AssessmentPeerReviewError.NotAttemptOwner => StatusCodes.Status403Forbidden,
            AssessmentPeerReviewError.NotCaseOwner => StatusCodes.Status403Forbidden,
            AssessmentPeerReviewError.NotAssignedVerifier => StatusCodes.Status403Forbidden,
            AssessmentPeerReviewError.NotAVerifier => StatusCodes.Status403Forbidden,
            AssessmentPeerReviewError.BlueprintNotFound => StatusCodes.Status404NotFound,
            AssessmentPeerReviewError.AttemptNotFound => StatusCodes.Status404NotFound,
            AssessmentPeerReviewError.CaseNotFound => StatusCodes.Status404NotFound,
            AssessmentPeerReviewError.VerifierProfileNotFound => StatusCodes.Status404NotFound,
            AssessmentPeerReviewError.BlueprintOutdated => StatusCodes.Status409Conflict,
            AssessmentPeerReviewError.AttemptAlreadySubmitted => StatusCodes.Status409Conflict,
            AssessmentPeerReviewError.NodeNotAvailable => StatusCodes.Status409Conflict,
            AssessmentPeerReviewError.OpenCaseAlreadyExists => StatusCodes.Status409Conflict,
            AssessmentPeerReviewError.CaseAlreadyResolved => StatusCodes.Status409Conflict,
            AssessmentPeerReviewError.CaseNotAssigned => StatusCodes.Status409Conflict,
            AssessmentPeerReviewError.SkillNotCompleted => StatusCodes.Status409Conflict,
            AssessmentPeerReviewError.VerifierSkillAlreadyEnabled => StatusCodes.Status409Conflict,
            AssessmentPeerReviewError.OperationCancelled => StatusCodes.Status409Conflict,
            AssessmentPeerReviewError.DatabaseError => StatusCodes.Status500InternalServerError,
            AssessmentPeerReviewError.InternalServerError => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest
        };
    }

    public static IActionResult ToActionResult<T>(
        ControllerBase controller,
        Result<T> result,
        ProblemDetailsFactory problemDetailsFactory,
        Func<T, IActionResult> successAction)
    {
        if (result.IsSuccess) return successAction(result.Value!);

        var statusCode = ToStatusCodeFromError((AssessmentPeerReviewError)result.Error!);
        return problemDetailsFactory.CreateProblemDetails(controller, statusCode, result.Error, result.Message,
            result.Details);
    }

    public static IActionResult ToErrorResult(
        ControllerBase controller,
        ProblemDetailsFactory problemDetailsFactory,
        IStringLocalizer<ErrorMessage> errorLocalizer,
        AssessmentPeerReviewError error)
    {
        return problemDetailsFactory.CreateProblemDetails(
            controller, ToStatusCodeFromError(error), error, errorLocalizer[error.ToString()]);
    }
}