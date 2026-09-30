using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SkillSwap.Platform.Iam.Domain.Model.Aggregates;

namespace SkillSwap.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;

/// <summary>
///     Decorates controllers and actions that require authorization.
///     It checks that the request authorization middleware resolved a user into
///     <c>HttpContext.Items["User"]</c> and, when <see cref="Roles" /> is set, that the
///     user's role is one of the allowed roles.
/// </summary>
/// <remarks>
///     Setting <see cref="Roles" /> at the action level (e.g. <c>[Authorize(Roles = "Coordinator")]</c>)
///     combines with a bare class-level <c>[Authorize]</c>: both filters run, so the request
///     must be authenticated AND match the role.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AuthorizeAttribute : Attribute, IAuthorizationFilter
{
    /// <summary>
    ///     Comma-separated list of roles allowed to access the decorated action.
    ///     When null or empty, any authenticated user is allowed.
    /// </summary>
    public string? Roles { get; set; }

    /// <inheritdoc />
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var allowAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
        if (allowAnonymous) return;

        var user = (User?)context.HttpContext.Items["User"];
        if (user is null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (string.IsNullOrWhiteSpace(Roles)) return;

        var allowedRoles = Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!allowedRoles.Contains(user.Role.ToString(), StringComparer.OrdinalIgnoreCase))
            context.Result = new ObjectResult(new { message = "Insufficient role for this operation." })
                { StatusCode = StatusCodes.Status403Forbidden };
    }
}