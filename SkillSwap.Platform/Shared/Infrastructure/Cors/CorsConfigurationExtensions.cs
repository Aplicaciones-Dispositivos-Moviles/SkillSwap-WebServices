namespace SkillSwap.Platform.Shared.Infrastructure.Cors;

public static class CorsConfigurationExtensions
{
    public const string PolicyName = "Default";
    public const string AllowedOriginsKey = "Cors:AllowedOrigins";

    /// <summary>
    ///     Registers the CORS policy of the API. Any origin is allowed until Cors:AllowedOrigins lists at least
    ///     one, from then on only those origins are. Native mobile apps do not depend on CORS.
    /// </summary>
    public static IServiceCollection AddConfiguredCors(this IServiceCollection services,
        IConfiguration configuration)
    {
        var origins = ReadOrigins(configuration);

        return services.AddCors(options => options.AddPolicy(PolicyName, policy =>
        {
            if (origins.Length == 0) policy.AllowAnyOrigin();
            else policy.WithOrigins(origins);

            policy.AllowAnyMethod().AllowAnyHeader();
        }));
    }

    /// <summary>
    ///     The origins from Cors:AllowedOrigins, given as an indexed list (Cors__AllowedOrigins__0, ...) or as
    ///     a single comma-separated value, without trailing slashes, blanks or duplicates.
    /// </summary>
    public static string[] ReadOrigins(IConfiguration configuration)
    {
        var values = new List<string>();

        var single = configuration[AllowedOriginsKey];
        if (!string.IsNullOrWhiteSpace(single)) values.Add(single);
        values.AddRange(configuration.GetSection(AllowedOriginsKey).Get<string[]>() ?? []);

        return values
            .SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(origin => origin.TrimEnd('/'))
            .Where(origin => origin.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}