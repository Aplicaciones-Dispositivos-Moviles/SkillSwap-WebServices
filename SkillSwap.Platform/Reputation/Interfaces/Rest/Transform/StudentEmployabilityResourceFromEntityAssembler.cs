using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Reputation.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.Reputation.Interfaces.Rest.Transform;

public static class StudentEmployabilityResourceFromEntityAssembler
{
    public static StudentEmployabilityResource ToResourceFromEntity(StudentEmployabilityScore entity)
    {
        return new StudentEmployabilityResource(
            entity.Id,
            entity.StudentId,
            entity.VerifiedSkillsCount,
            entity.Score.Value,
            entity.UpdatedAt);
    }
}