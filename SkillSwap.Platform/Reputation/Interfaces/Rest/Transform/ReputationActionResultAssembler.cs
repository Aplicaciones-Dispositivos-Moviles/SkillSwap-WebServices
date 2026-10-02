using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using SkillSwap.Platform.Reputation.Domain.Model;
using SkillSwap.Platform.Shared.Interfaces.Rest.ProblemDetails;
using SkillSwap.Platform.Shared.Resources.Errors;

namespace SkillSwap.Platform.Reputation.Interfaces.Rest.Transform;

/// <summary>
///     Maps domain errors to HTTP responses
/// </summary>
public static class ReputationActionResultAssembler
{
    private static int ToStatusCodeFromError(ReputationError error)
    {
        return error switch
        {
            ReputationError.ReputationNotFound => StatusCodes.Status404NotFound,
            ReputationError.NotReputationOwner => StatusCodes.Status403Forbidden,
            ReputationError.OperationCancelled => StatusCodes.Status409Conflict,
            ReputationError.DatabaseError => StatusCodes.Status500InternalServerError,
            ReputationError.InternalServerError => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest
        };
    }

    public static IActionResult ToErrorResult(
        ControllerBase controller,
        ProblemDetailsFactory problemDetailsFactory,
        IStringLocalizer<ErrorMessage> errorLocalizer,
        ReputationError error)
    {
        return problemDetailsFactory.CreateProblemDetails(
            controller, ToStatusCodeFromError(error), error, errorLocalizer[error.ToString()]);
    }
}