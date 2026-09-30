using SkillSwap.Platform.Iam.Application.Internal.OutboundServices;
using SkillSwap.Platform.Iam.Application.QueryServices;
using SkillSwap.Platform.Iam.Domain.Model.Queries;
using SkillSwap.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;

namespace SkillSwap.Platform.Iam.Infrastructure.Pipeline.Middleware.Components;

/// <summary>
///     Resolves the Bearer JWT on each request (unless the endpoint is [AllowAnonymous]) and
///     stores the corresponding <see cref="Iam.Domain.Model.Aggregates.User" /> in
///     <c>HttpContext.Items["User"]</c> for <see cref="AuthorizeAttribute" /> to consume.
/// </summary>
/// <param name="next">The next middleware in the pipeline</param>
public class RequestAuthorizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IUserQueryService userQueryService,
        ITokenGenerator tokenGenerator)
    {
        var allowAnonymous = context.GetEndpoint()?.Metadata
            .Any(m => m.GetType() == typeof(AllowAnonymousAttribute)) ?? false;
        if (allowAnonymous)
        {
            await next(context);
            return;
        }

        var token = context.Request.Headers.Authorization.FirstOrDefault()?.Split(' ').Last();
        var userId = token is null ? null : await tokenGenerator.ValidateToken(token);

        if (userId is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var user = await userQueryService.Handle(new GetUserByIdQuery(userId.Value), context.RequestAborted);
        context.Items["User"] = user;

        await next(context);
    }
}