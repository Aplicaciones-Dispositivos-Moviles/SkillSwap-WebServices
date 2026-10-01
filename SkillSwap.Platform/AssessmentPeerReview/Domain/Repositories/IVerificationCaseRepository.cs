// IVerificationCaseRepository.cs
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.Shared.Domain.Repositories;

namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Repositories;

public interface IVerificationCaseRepository : IBaseRepository<VerificationCase>
{
    /// <summary>
    ///     The cases assigned to a verifier, newest first.
    /// </summary>
    Task<IReadOnlyList<VerificationCase>> FindByVerifierUserIdAsync(int verifierUserId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     The case of the student for the node that is not resolved yet, if any.
    /// </summary>
    Task<VerificationCase?> FindOpenByStudentAndNodeAsync(int studentId, int pathNodeId,
        CancellationToken cancellationToken);

    /// <summary>
    ///     The pending cases of a skill, oldest first.
    /// </summary>
    Task<IReadOnlyList<VerificationCase>> FindPendingBySkillTagAsync(string skillTag,
        CancellationToken cancellationToken);

    /// <summary>
    ///     How many unresolved cases each verifier has. Verifiers without cases map to zero.
    /// </summary>
    Task<IReadOnlyDictionary<int, int>> CountOpenByVerifierUserIdsAsync(IReadOnlyCollection<int> verifierUserIds,
        CancellationToken cancellationToken);
}