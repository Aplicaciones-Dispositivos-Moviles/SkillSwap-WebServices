using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using SkillSwap.Platform.CredentialVerification.Domain.Model;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Interfaces.Rest.ProblemDetails;
using SkillSwap.Platform.Shared.Resources.Errors;

namespace SkillSwap.Platform.CredentialVerification.Interfaces.Rest.Transform;

/// <summary>
///     Maps application results and domain errors to HTTP responses
/// </summary>
public static class CredentialVerificationActionResultAssembler
{
    private static int ToStatusCodeFromError(CredentialVerificationError error)
    {
        return error switch
        {
            CredentialVerificationError.FileRequired => StatusCodes.Status400BadRequest,
            CredentialVerificationError.InvalidFileType => StatusCodes.Status415UnsupportedMediaType,
            CredentialVerificationError.FileTooLarge => StatusCodes.Status413PayloadTooLarge,
            CredentialVerificationError.DuplicateFile => StatusCodes.Status409Conflict,
            CredentialVerificationError.FieldTooLong => StatusCodes.Status400BadRequest,
            CredentialVerificationError.CertificateNotFound => StatusCodes.Status404NotFound,
            CredentialVerificationError.NotCertificateOwner => StatusCodes.Status403Forbidden,
            CredentialVerificationError.InvalidStatusTransition => StatusCodes.Status409Conflict,
            CredentialVerificationError.StorageError => StatusCodes.Status502BadGateway,
            CredentialVerificationError.OperationCancelled => StatusCodes.Status409Conflict,
            CredentialVerificationError.DatabaseError => StatusCodes.Status500InternalServerError,
            CredentialVerificationError.InternalServerError => StatusCodes.Status500InternalServerError,
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

        var statusCode = ToStatusCodeFromError((CredentialVerificationError)result.Error!);
        return problemDetailsFactory.CreateProblemDetails(controller, statusCode, result.Error, result.Message,
            result.Details);
    }

    public static IActionResult ToErrorResult(
        ControllerBase controller,
        ProblemDetailsFactory problemDetailsFactory,
        IStringLocalizer<ErrorMessage> errorLocalizer,
        CredentialVerificationError error)
    {
        return problemDetailsFactory.CreateProblemDetails(
            controller, ToStatusCodeFromError(error), error, errorLocalizer[error.ToString()]);
    }
}