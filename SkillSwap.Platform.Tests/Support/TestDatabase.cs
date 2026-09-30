using System.Text.Json;
using Npgsql;

namespace SkillSwap.Platform.Tests.Support;

/// <summary>
///     Resolves the connection string of the integration-test database.
/// </summary>
/// <remarks>
///     Uses SKILLSWAP_TEST_CONNECTION when set. Otherwise it takes the development connection string
///     from the API's appsettings.Development.json and points it to "skillswap_mobile_test".
///     The database name must end with "_test": the tests truncate every table, so they refuse to
///     run against anything else.
/// </remarks>
public static class TestDatabase
{
    private const string DefaultTestDatabase = "skillswap_mobile_test";

    public static string ConnectionString { get; } = Resolve();

    private static string Resolve()
    {
        var explicitConnection = Environment.GetEnvironmentVariable("SKILLSWAP_TEST_CONNECTION");
        var builder = new NpgsqlConnectionStringBuilder(explicitConnection ?? ReadDevelopmentConnectionString());

        if (explicitConnection is null) builder.Database = DefaultTestDatabase;

        if (builder.Database is null || !builder.Database.EndsWith("_test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Refusing to run integration tests against database '{builder.Database}': its name must end with '_test'.");

        return builder.ConnectionString;
    }

    private static string ReadDevelopmentConnectionString()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "SkillSwap.Platform", "appsettings.Development.json");
            if (File.Exists(candidate))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(candidate));
                return document.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection")
                    .GetString()!;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not find SkillSwap.Platform/appsettings.Development.json. " +
            "Set the SKILLSWAP_TEST_CONNECTION environment variable instead.");
    }
}