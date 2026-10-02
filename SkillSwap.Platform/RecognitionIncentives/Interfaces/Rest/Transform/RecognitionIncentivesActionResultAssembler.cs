using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Interfaces.Rest.ProblemDetails;
using SkillSwap.Platform.Shared.Resources.Errors;

namespace SkillSwap.Platform.RecognitionIncentives.Interfaces.Rest.Transform;

/// <summary>
///     Maps application results and domain errors to HTTP responses
/// </summary>
public static class RecognitionIncentivesActionResultAssembler
{
    private static int ToStatusCodeFromError(RecognitionIncentivesError error)
    {
        return error switch
        {
            RecognitionIncentivesError.InvalidRedemptionItem => StatusCodes.Status400BadRequest,
            RecognitionIncentivesError.NotWalletOwner => StatusCodes.Status403Forbidden,
            RecognitionIncentivesError.WalletNotFound => StatusCodes.Status404NotFound,
            RecognitionIncentivesError.InsufficientBalance => StatusCodes.Status409Conflict,
            RecognitionIncentivesError.ConcurrentUpdate => StatusCodes.Status409Conflict,
            RecognitionIncentivesError.OperationCancelled => StatusCodes.Status409Conflict,
            RecognitionIncentivesError.DatabaseError => StatusCodes.Status500InternalServerError,
            RecognitionIncentivesError.InternalServerError => StatusCodes.Status500InternalServerError,
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

        var statusCode = ToStatusCodeFromError((RecognitionIncentivesError)result.Error!);
        return problemDetailsFactory.CreateProblemDetails(controller, statusCode, result.Error, result.Message,
            result.Details);
    }

    public static IActionResult ToErrorResult(
        ControllerBase controller,
        ProblemDetailsFactory problemDetailsFactory,
        IStringLocalizer<ErrorMessage> errorLocalizer,
        RecognitionIncentivesError error)
    {
        return problemDetailsFactory.CreateProblemDetails(
            controller, ToStatusCodeFromError(error), error, errorLocalizer[error.ToString()]);
    }
}