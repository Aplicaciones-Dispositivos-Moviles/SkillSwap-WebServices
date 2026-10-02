namespace SkillSwap.Platform.Iam.Infrastructure.Seeding;

public static class CoordinatorSeedingExtensions
{
    /// <summary>
    ///     Runs the <see cref="CoordinatorSeeder" /> in its own scope. Call it after the migrations.
    /// </summary>
    public static async Task<CoordinatorSeedOutcome> SeedCoordinatorAsync(this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<CoordinatorSeeder>().SeedAsync(cancellationToken);
    }
}