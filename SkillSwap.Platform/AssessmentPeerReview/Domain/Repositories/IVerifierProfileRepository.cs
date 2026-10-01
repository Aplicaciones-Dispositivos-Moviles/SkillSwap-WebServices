// IVerifierProfileRepository.cs
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.Shared.Domain.Repositories;

namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Repositories;

public interface IVerifierProfileRepository : IBaseRepository<VerifierProfile>
{
    Task<VerifierProfile?> FindByUserIdAsync(int userId, CancellationToken cancellationToken);

    /// <summary>
    ///     The enabled verifiers (not revoked) whose profile includes the skill, available or not.
    /// </summary>
    Task<IReadOnlyList<VerifierProfile>> FindEnabledBySkillTagAsync(string skillTag,
        CancellationToken cancellationToken);
}