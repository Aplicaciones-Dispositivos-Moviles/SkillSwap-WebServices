using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.CredentialVerification.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using SkillSwap.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using SkillSwap.Platform.LearningPathEngine.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using SkillSwap.Platform.AssessmentPeerReview.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using SkillSwap.Platform.Reputation.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

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

        builder.ApplyIamConfiguration();
        builder.ApplyCredentialVerificationConfiguration();
        builder.ApplyLearningPathEngineConfiguration();
        builder.ApplyLearningPathEngineConfiguration();
        builder.ApplyAssessmentPeerReviewConfiguration();
        builder.ApplyReputationConfiguration();
    }
}