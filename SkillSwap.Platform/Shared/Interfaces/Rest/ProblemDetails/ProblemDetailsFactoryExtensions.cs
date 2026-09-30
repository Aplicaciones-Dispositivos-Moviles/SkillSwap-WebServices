using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace SkillSwap.Platform.Shared.Interfaces.Rest.ProblemDetails;

/// <summary>
///     Extension methods over the built-in ASP.NET Core <see cref="ProblemDetailsFactory" />
///     to build a consistent error response from a domain error enum and a localized message.
/// </summary>
public static class ProblemDetailsFactoryExtensions
{
    /// <summary>
    ///     Creates an <see cref="IActionResult" /> wrapping a <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails" />
    ///     object, using the given status code, domain error and message.
    /// </summary>
    public static IActionResult CreateProblemDetails(
        this ProblemDetailsFactory problemDetailsFactory,
        ControllerBase controller,
        int statusCode,
        Enum? error,
        string message)
    {
        var problemDetails = problemDetailsFactory.CreateProblemDetails(
            controller.HttpContext,
            statusCode,
            title: error?.ToString() ?? "Error",
            detail: message
        );

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}