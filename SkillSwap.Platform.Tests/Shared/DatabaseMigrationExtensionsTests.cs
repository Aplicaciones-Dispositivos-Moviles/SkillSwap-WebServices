using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.Shared;

public class DatabaseMigrationExtensionsTests
{
    private static IConfiguration Configuration(string? migrateOnStartup)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [DatabaseMigrationExtensions.MigrateOnStartupKey] = migrateOnStartup
            })
            .Build();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    [InlineData("")]
    [InlineData("yes")]
    public async Task Migrate_WhenTheOptionIsOff_DoesNothing(string? value)
    {
        var applied = await TestApi.Factory.Services.MigrateDatabaseIfEnabledAsync(Configuration(value));

        Assert.False(applied);
    }

    [Fact]
    public async Task Migrate_WhenTheOptionIsOnAndTheDatabaseIsUpToDate_SucceedsWithoutPendingMigrations()
    {
        var applied = await TestApi.Factory.Services.MigrateDatabaseIfEnabledAsync(Configuration("true"));

        Assert.True(applied);
        using var scope = TestApi.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }
}