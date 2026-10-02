using Npgsql;

namespace SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Converts the connection URL that hosting platforms such as Render provide
///     (postgres://user:password@host:port/database) into an Npgsql connection string.
/// </summary>
public static class PostgresUrlConverter
{
    public const int DefaultPort = 5432;

    /// <summary>
    ///     SSL is required for external hosts (the name has dots) and preferred for internal ones, which are
    ///     reached through the platform's private network by a short name.
    /// </summary>
    /// <exception cref="FormatException">Thrown when the URL is not a valid PostgreSQL URL.</exception>
    public static string ToNpgsqlConnectionString(string databaseUrl)
    {
        const string expected = "The database URL must look like postgres://user:password@host:port/database.";

        if (string.IsNullOrWhiteSpace(databaseUrl)
            || !Uri.TryCreate(databaseUrl.Trim(), UriKind.Absolute, out var uri)
            || uri.Scheme is not ("postgres" or "postgresql")
            || string.IsNullOrEmpty(uri.Host))
            throw new FormatException(expected);

        var credentials = uri.UserInfo.Split(':', 2);
        var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        if (credentials.Length < 2 || credentials[0].Length == 0 || credentials[1].Length == 0
            || database.Length == 0)
            throw new FormatException(expected);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : DefaultPort,
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1]),
            Database = database,
            SslMode = uri.Host.Contains('.') ? SslMode.Require : SslMode.Prefer,
            TrustServerCertificate = true
        };
        return builder.ConnectionString;
    }
}