using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Commands;
using SkillSwap.Platform.Shared.Application.Model;

namespace SkillSwap.Platform.LearningPathEngine.Application.CommandServices;

/// <summary>
///     Learning path command service interface
/// </summary>
public interface ILearningPathCommandService
{
    /// <summary>
    ///     Handle declare goal command: interprets the goal, computes the gap and creates the path.
    /// </summary>
    Task<Result<LearningPath>> Handle(DeclareGoalCommand command, CancellationToken cancellationToken);

    /// <summary>
    ///     Handle complete path node command, issued once the student approved the node's assessment.
    /// </summary>
    Task<Result<LearningPath>> Handle(CompletePathNodeCommand command, CancellationToken cancellationToken);
}