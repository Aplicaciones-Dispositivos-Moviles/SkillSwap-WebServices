using Microsoft.OpenApi.Models;
using SkillSwap.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SkillSwap.Platform.Shared.Infrastructure.OpenApi;

/// <summary>
///     Adds the Bearer security requirement to a Swagger operation unless the action is
///     decorated with [AllowAnonymous], so the lock icon only shows on endpoints that require a JWT.
/// </summary>
public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var allowAnonymous = context.MethodInfo.GetCustomAttributes(true)
            .OfType<AllowAnonymousAttribute>()
            .Any();
        if (allowAnonymous) return;

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    }
}