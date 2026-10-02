using SkillSwap.Platform.Reputation.Application.QueryServices;
using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Reputation.Domain.Model.Queries;
using SkillSwap.Platform.Reputation.Domain.Repositories;

namespace SkillSwap.Platform.Reputation.Application.Internal.QueryServices;

/// <summary>
///     Verifier reliability query service
/// </summary>
/// <param name="reliabilityRepository">Verifier reliability repository</param>
public class VerifierReliabilityQueryService(IVerifierReliabilityRepository reliabilityRepository)
    : IVerifierReliabilityQueryService
{
    /// <inheritdoc />
    public async Task<VerifierReliability?> Handle(GetVerifierReliabilityByUserIdQuery query,
        CancellationToken cancellationToken)
    {
        return await reliabilityRepository.FindByVerifierUserIdAsync(query.VerifierUserId, cancellationToken);
    }
}