using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using SkillSwap.Platform.LearningPathEngine.Domain.Model;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Interfaces.Rest.ProblemDetails;
using SkillSwap.Platform.Shared.Resources.Errors;

namespace SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Transform;

/// <summary>
///     Maps application results and domain errors to HTTP responses
/// </summary>
public static class LearningPathActionResultAssembler
{
    private static int ToStatusCodeFromError(LearningPathError error)
    {
        return error switch
        {
            LearningPathError.InvalidGoal => StatusCodes.Status400BadRequest,
            LearningPathError.GoalNotInterpretable => StatusCodes.Status422UnprocessableEntity,
            LearningPathError.GoalAlreadyAchieved => StatusCodes.Status409Conflict,
            LearningPathError.ActivePathAlreadyExists => StatusCodes.Status409Conflict,
            LearningPathError.PathNotFound => StatusCodes.Status404NotFound,
            LearningPathError.NotPathOwner => StatusCodes.Status403Forbidden,
            LearningPathError.NodeNotFound => StatusCodes.Status404NotFound,
            LearningPathError.NodeLocked => StatusCodes.Status409Conflict,
            LearningPathError.NodeAlreadyCompleted => StatusCodes.Status409Conflict,
            LearningPathError.QuestionGenerationFailed => StatusCodes.Status503ServiceUnavailable,
            LearningPathError.OperationCancelled => StatusCodes.Status409Conflict,
            LearningPathError.DatabaseError => StatusCodes.Status500InternalServerError,
            LearningPathError.InternalServerError => StatusCodes.Status500InternalServerError,
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

        var statusCode = ToStatusCodeFromError((LearningPathError)result.Error!);
        return problemDetailsFactory.CreateProblemDetails(controller, statusCode, result.Error, result.Message,
            result.Details);
    }

    public static IActionResult ToErrorResult(
        ControllerBase controller,
        ProblemDetailsFactory problemDetailsFactory,
        IStringLocalizer<ErrorMessage> errorLocalizer,
        LearningPathError error)
    {
        return problemDetailsFactory.CreateProblemDetails(
            controller, ToStatusCodeFromError(error), error, errorLocalizer[error.ToString()]);
    }
}