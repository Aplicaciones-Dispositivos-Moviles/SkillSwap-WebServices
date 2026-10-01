using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Transform;

public static class AssessmentBlueprintResourceFromEntityAssembler
{
    /// <remarks>
    ///     The correct answer of each question is omitted on purpose. It stays on the server so Assessment
    ///     &amp; Peer Review can grade the attempt.
    /// </remarks>
    public static AssessmentBlueprintResource ToResourceFromEntity(AssessmentBlueprint entity,
        Func<string, string> skillNameOf)
    {
        return new AssessmentBlueprintResource(
            entity.Id,
            entity.PathNodeId,
            entity.SkillTag,
            skillNameOf(entity.SkillTag),
            entity.Questions.Select(q => new QuestionResource(q.QuestionString, q.Answers)).ToList(),
            entity.GeneratedAt);
    }
}