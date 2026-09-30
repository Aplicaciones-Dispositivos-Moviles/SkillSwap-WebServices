using Microsoft.AspNetCore.Localization;

namespace SkillSwap.Platform.Shared.Infrastructure.Localization;

/// <summary>
///     Maps any Spanish variant requested through Accept-Language (es, es-PE, es-MX, ...)
///     to Latin American Spanish (es-419), the project's secondary language.
/// </summary>
public class LatinAmericanSpanishRequestCultureProvider : RequestCultureProvider
{
    public const string Culture = "es-419";

    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var preferred = httpContext.Request.GetTypedHeaders().AcceptLanguage
            .OrderByDescending(language => language.Quality ?? 1)
            .Select(language => language.Value.Value)
            .FirstOrDefault();

        var isSpanish = preferred is not null
                        && preferred.StartsWith("es", StringComparison.OrdinalIgnoreCase);

        return Task.FromResult(isSpanish ? new ProviderCultureResult(Culture) : null);
    }
}