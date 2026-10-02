using Npgsql;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace SkillSwap.Platform.Tests.Shared;

public class PostgresUrlConverterTests
{
    private static NpgsqlConnectionStringBuilder Convert(string url)
    {
        return new NpgsqlConnectionStringBuilder(PostgresUrlConverter.ToNpgsqlConnectionString(url));
    }

    [Fact]
    public void ToNpgsqlConnectionString_WithAnExternalUrl_KeepsEveryPartAndRequiresSsl()
    {
        var connection = Convert(
            "postgresql://skill_user:s3cret@dpg-abc123-a.virginia-postgres.render.com:5433/skillswap_db");

        Assert.Equal("dpg-abc123-a.virginia-postgres.render.com", connection.Host);
        Assert.Equal(5433, connection.Port);
        Assert.Equal("skill_user", connection.Username);
        Assert.Equal("s3cret", connection.Password);
        Assert.Equal("skillswap_db", connection.Database);
        Assert.Equal(SslMode.Require, connection.SslMode);
        Assert.True(connection.TrustServerCertificate);
    }

    [Fact]
    public void ToNpgsqlConnectionString_WithAnInternalUrl_UsesTheDefaultPortAndPrefersSsl()
    {
        var connection = Convert("postgres://skill_user:s3cret@dpg-abc123-a/skillswap_db");

        Assert.Equal("dpg-abc123-a", connection.Host);
        Assert.Equal(5432, connection.Port);
        Assert.Equal(SslMode.Prefer, connection.SslMode);
    }

    [Fact]
    public void ToNpgsqlConnectionString_DecodesSpecialCharactersOfTheCredentials()
    {
        var connection = Convert("postgres://sk%40ll:p%40ss%3Aword%2F1@dpg-abc123-a/skillswap_db");

        Assert.Equal("sk@ll", connection.Username);
        Assert.Equal("p@ss:word/1", connection.Password);
    }

    [Fact]
    public void ToNpgsqlConnectionString_IgnoresTheQueryString()
    {
        var connection = Convert("postgres://skill_user:s3cret@dpg-abc123-a/skillswap_db?sslmode=require");

        Assert.Equal("skillswap_db", connection.Database);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("mysql://user:pass@host/db")]
    [InlineData("postgres://host/db")]
    [InlineData("postgres://user@host/db")]
    [InlineData("postgres://user:pass@host")]
    [InlineData("postgres://user:pass@host/")]
    public void ToNpgsqlConnectionString_WithAnInvalidUrl_ThrowsFormatException(string url)
    {
        Assert.Throws<FormatException>(() => PostgresUrlConverter.ToNpgsqlConnectionString(url));
    }
}