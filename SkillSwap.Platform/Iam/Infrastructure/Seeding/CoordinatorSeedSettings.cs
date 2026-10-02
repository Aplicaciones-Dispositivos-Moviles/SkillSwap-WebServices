namespace SkillSwap.Platform.Iam.Infrastructure.Seeding;

/// <summary>
///     Account of the Coordinator created at startup (section Seed:Coordinator; environment variables
///     Seed__Coordinator__Username, Seed__Coordinator__Email and Seed__Coordinator__Password).
///     Coordinators cannot sign up, so this is how the first one exists in a hosted environment.
/// </summary>
public class CoordinatorSeedSettings
{
    public string? Username { get; set; }

    /// <summary>
    ///     Must be an institutional (.edu.pe) address, like every account.
    /// </summary>
    public string? Email { get; set; }

    public string? Password { get; set; }
}