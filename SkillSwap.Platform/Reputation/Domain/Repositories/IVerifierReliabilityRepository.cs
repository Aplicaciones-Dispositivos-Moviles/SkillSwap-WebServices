using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Shared.Domain.Repositories;

namespace SkillSwap.Platform.Reputation.Domain.Repositories;

public interface IVerifierReliabilityRepository : IBaseRepository<VerifierReliability>
{
    Task<VerifierReliability?> FindByVerifierUserIdAsync(int verifierUserId, CancellationToken cancellationToken);
}