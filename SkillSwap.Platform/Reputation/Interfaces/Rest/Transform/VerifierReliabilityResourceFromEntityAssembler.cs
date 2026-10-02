using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Reputation.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.Reputation.Interfaces.Rest.Transform;

public static class VerifierReliabilityResourceFromEntityAssembler
{
    public static VerifierReliabilityResource ToResourceFromEntity(VerifierReliability entity)
    {
        return new VerifierReliabilityResource(
            entity.Id,
            entity.VerifierUserId,
            entity.ResolvedCasesCount,
            entity.OverturnedDecisionsCount,
            entity.SanctionsCount,
            entity.Score.Value,
            entity.UpdatedAt);
    }
}