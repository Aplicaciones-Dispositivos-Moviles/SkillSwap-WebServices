using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Transform;

public static class LearningPathResourceFromEntityAssembler
{
    public static LearningPathResource ToResourceFromEntity(LearningPath entity, Func<string, string> skillNameOf)
    {
        var nodes = entity.Nodes
            .Select(node => new PathNodeResource(
                node.Id,
                node.SkillTag,
                skillNameOf(node.SkillTag),
                node.Order,
                node.Status.ToString(),
                node.PrerequisiteSkillTags,
                node.LinkedCertificateId,
                node.AssessmentBlueprintId))
            .ToList();

        return new LearningPathResource(
            entity.Id,
            entity.StudentId,
            entity.CareerGoal.RawText,
            entity.CareerGoal.MappedSkillTags,
            entity.Status.ToString(),
            nodes,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}