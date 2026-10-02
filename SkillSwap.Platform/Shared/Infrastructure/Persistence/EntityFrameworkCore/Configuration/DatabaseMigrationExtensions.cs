using Microsoft.EntityFrameworkCore;

namespace SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public static class DatabaseMigrationExtensions
{
    public const string MigrateOnStartupKey = "Database:MigrateOnStartup";

    /// <summary>
    ///     Applies the pending migrations when Database:MigrateOnStartup is true (environment variable
    ///     Database__MigrateOnStartup). It is meant for the hosted environment: local development keeps
    ///     applying migrations by hand.
    /// </summary>
    /// <returns>True when the migrations were applied; false when the option is off.</returns>
    public static async Task<bool> MigrateDatabaseIfEnabledAsync(this IServiceProvider services,
        IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        // Only an explicit "true" turns it on; an empty or malformed value leaves migrations off.
        if (!bool.TryParse(configuration[MigrateOnStartupKey], out var enabled) || !enabled) return false;
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        logger.LogInformation("Applying pending database migrations.");
        await context.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Database migrations applied.");
        return true;
    }
}