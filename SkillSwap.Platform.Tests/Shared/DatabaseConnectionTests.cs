using Microsoft.Extensions.Configuration;
using Npgsql;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace SkillSwap.Platform.Tests.Shared;

public class DatabaseConnectionTests
{
    private const string Url = "postgres://skill_user:s3cret@dpg-abc123-a/skillswap_db";

    private static IConfiguration Configuration(params (string Key, string? Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(v => v.Key, v => v.Value))
            .Build();
    }

    [Fact]
    public void Resolve_PrefersTheConfiguredConnectionString()
    {
        var configuration = Configuration(
            ("ConnectionStrings:DefaultConnection", "Host=localhost;Database=skillswap_mobile"),
            ("DATABASE_URL", Url));

        Assert.Equal("Host=localhost;Database=skillswap_mobile", DatabaseConnection.Resolve(configuration));
    }

    [Fact]
    public void Resolve_WithoutAConfiguredConnection_ConvertsTheDatabaseUrl()
    {
        var connection = DatabaseConnection.Resolve(Configuration(("DATABASE_URL", Url)));

        var parsed = new NpgsqlConnectionStringBuilder(connection);
        Assert.Equal("dpg-abc123-a", parsed.Host);
        Assert.Equal("skillswap_db", parsed.Database);
    }

    [Fact]
    public void Resolve_WithABlankConfiguredConnection_FallsBackToTheDatabaseUrl()
    {
        var connection = DatabaseConnection.Resolve(
            Configuration(("ConnectionStrings:DefaultConnection", "  "), ("DATABASE_URL", Url)));

        Assert.Equal("skillswap_db", new NpgsqlConnectionStringBuilder(connection).Database);
    }

    [Fact]
    public void Resolve_WithNeitherSource_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => DatabaseConnection.Resolve(Configuration()));
    }

    [Fact]
    public void Resolve_WithAnInvalidDatabaseUrl_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => DatabaseConnection.Resolve(Configuration(("DATABASE_URL", "nope"))));
    }
}