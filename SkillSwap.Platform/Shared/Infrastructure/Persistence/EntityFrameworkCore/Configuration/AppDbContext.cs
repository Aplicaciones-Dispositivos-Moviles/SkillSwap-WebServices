using Microsoft.EntityFrameworkCore;

namespace SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Application database context.
/// </summary>
/// <remarks>
///     Each bounded context registers its own entity configuration via an extension method
///     on ModelBuilder, called from OnModelCreating below.
/// </remarks>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Bounded context configurations are registered here as each one is implemented.
    }
}