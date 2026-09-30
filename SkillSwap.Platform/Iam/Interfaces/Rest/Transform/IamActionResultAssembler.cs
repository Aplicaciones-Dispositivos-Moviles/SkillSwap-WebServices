using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using SkillSwap.Platform.Iam.Domain.Model;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Interfaces.Rest.ProblemDetails;
using SkillSwap.Platform.Shared.Resources.Errors;

namespace SkillSwap.Platform.Iam.Interfaces.Rest.Transform;

/// <summary>
///     Maps application results and domain errors to HTTP responses
/// </summary>
public static class IamActionResultAssembler
{
    private static int ToStatusCodeFromIamError(IamError error)
    {
        return error switch
        {
            IamError.InvalidCredentials => StatusCodes.Status401Unauthorized,
            IamError.UserBanned => StatusCodes.Status403Forbidden,
            IamError.UsernameAlreadyTaken => StatusCodes.Status409Conflict,
            IamError.EmailAlreadyTaken => StatusCodes.Status409Conflict,
            IamError.InvalidInstitutionalEmail => StatusCodes.Status400BadRequest,
            IamError.InvalidUsername => StatusCodes.Status400BadRequest,
            IamError.WeakPassword => StatusCodes.Status400BadRequest,
            IamError.BioTooLong => StatusCodes.Status400BadRequest,
            IamError.UserNotFound => StatusCodes.Status404NotFound,
            IamError.NotProfileOwner => StatusCodes.Status403Forbidden,
            IamError.OperationCancelled => StatusCodes.Status409Conflict,
            IamError.DatabaseError => StatusCodes.Status500InternalServerError,
            IamError.InternalServerError => StatusCodes.Status500InternalServerError,
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

        var statusCode = ToStatusCodeFromIamError((IamError)result.Error!);
        return problemDetailsFactory.CreateProblemDetails(controller, statusCode, result.Error, result.Message);
    }

    public static IActionResult ToUserNotFoundResult(
        ControllerBase controller,
        ProblemDetailsFactory problemDetailsFactory,
        IStringLocalizer<ErrorMessage> errorLocalizer)
    {
        return problemDetailsFactory.CreateProblemDetails(
            controller,
            ToStatusCodeFromIamError(IamError.UserNotFound),
            IamError.UserNotFound,
            errorLocalizer[nameof(IamError.UserNotFound)]);
    }
}