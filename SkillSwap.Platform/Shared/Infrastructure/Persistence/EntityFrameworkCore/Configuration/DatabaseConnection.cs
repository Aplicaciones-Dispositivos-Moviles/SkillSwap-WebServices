namespace SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Decides which database connection the API uses.
/// </summary>
public static class DatabaseConnection
{
    /// <summary>
    ///     ConnectionStrings:DefaultConnection when it is set (local development and tests); otherwise the
    ///     DATABASE_URL provided by the hosting platform, converted to an Npgsql connection string.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when neither is configured.</exception>
    public static string Resolve(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(configured)) return configured;

        var url = configuration["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(url)) return PostgresUrlConverter.ToNpgsqlConnectionString(url);

        throw new InvalidOperationException(
            "Neither ConnectionStrings:DefaultConnection nor DATABASE_URL is configured.");
    }
}