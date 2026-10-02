using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Reputation.Domain.Model.Queries;

namespace SkillSwap.Platform.Reputation.Application.QueryServices;

public interface IVerifierReliabilityQueryService
{
    Task<VerifierReliability?> Handle(GetVerifierReliabilityByUserIdQuery query,
        CancellationToken cancellationToken);
}