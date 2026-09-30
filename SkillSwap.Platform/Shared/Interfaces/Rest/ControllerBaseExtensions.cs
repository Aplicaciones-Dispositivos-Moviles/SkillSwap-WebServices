using Microsoft.AspNetCore.Mvc;
using SkillSwap.Platform.Iam.Domain.Model.Aggregates;

namespace SkillSwap.Platform.Shared.Interfaces.Rest;

/// <summary>
///     Extension methods to read the authenticated caller's identity, resolved by the request
///     authorization middleware into <c>HttpContext.Items["User"]</c>.
/// </summary>
/// <remarks>
///     Controllers should use this instead of trusting an actor id submitted in the request body,
///     which the client fully controls.
/// </remarks>
public static class ControllerBaseExtensions
{
    /// <summary>
    ///     The id of the currently authenticated user, as resolved from the Bearer token.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if called on an action that isn't behind the request authorization middleware.
    /// </exception>
    public static int CurrentUserId(this ControllerBase controller)
    {
        var user = (User?)controller.HttpContext.Items["User"];
        return user?.Id ?? throw new InvalidOperationException("No authenticated user in context.");
    }
}