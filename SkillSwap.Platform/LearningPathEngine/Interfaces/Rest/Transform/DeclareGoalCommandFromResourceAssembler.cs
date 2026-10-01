using SkillSwap.Platform.LearningPathEngine.Domain.Model.Commands;
using SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Resources;

namespace SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Transform;

public static class DeclareGoalCommandFromResourceAssembler
{
    /// <remarks>
    ///     The student is always the authenticated user, never a value from the request body.
    /// </remarks>
    public static DeclareGoalCommand ToCommandFromResource(DeclareGoalResource resource, int studentId)
    {
        return new DeclareGoalCommand(studentId, resource.Goal ?? string.Empty);
    }
}