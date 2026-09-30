namespace SkillSwap.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;

/// <summary>
///     Decorates an action that does not require authorization, letting it skip the
///     <see cref="AuthorizeAttribute" /> checks and the request authorization middleware.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AllowAnonymousAttribute : Attribute;