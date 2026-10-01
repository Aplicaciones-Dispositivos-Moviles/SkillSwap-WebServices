using SkillSwap.Platform.LearningPathEngine.Application.CommandServices;
using SkillSwap.Platform.LearningPathEngine.Domain.Model;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Commands;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;
using SkillSwap.Platform.LearningPathEngine.Domain.Repositories;

namespace SkillSwap.Platform.LearningPathEngine.Application.ACL;

/// <summary>
///     Facade implementation over the learning path repositories and command service
/// </summary>
/// <param name="learningPathRepository">Learning path repository</param>
/// <param name="blueprintRepository">Assessment blueprint repository</param>
/// <param name="learningPathCommandService">Learning path command service</param>
public class LearningPathContextFacade(
    ILearningPathRepository learningPathRepository,
    IAssessmentBlueprintRepository blueprintRepository,
    ILearningPathCommandService learningPathCommandService) : ILearningPathContextFacade
{
    /// <inheritdoc />
    public async Task<BlueprintView?> GetBlueprintAsync(int blueprintId, CancellationToken cancellationToken)
    {
        var blueprint = await blueprintRepository.FindByIdAsync(blueprintId, cancellationToken);
        if (blueprint is null) return null;

        var path = await learningPathRepository.FindByNodeIdAsync(blueprint.PathNodeId, cancellationToken);
        var node = path?.GetNode(blueprint.PathNodeId);
        if (path is null || node is null) return null;

        var questions = blueprint.Questions
            .Select(q => new BlueprintQuestionView(q.QuestionString, q.Answers, q.CorrectAnswer))
            .ToList();

        return new BlueprintView(
            blueprint.Id,
            blueprint.PathNodeId,
            path.StudentId,
            blueprint.SkillTag,
            node.AssessmentBlueprintId == blueprint.Id,
            node.Status == NodeStatus.Available,
            questions);
    }

    /// <inheritdoc />
    public async Task<NodeCompletionOutcome> CompleteNodeAsync(int pathNodeId, CancellationToken cancellationToken)
    {
        var result = await learningPathCommandService.Handle(new CompletePathNodeCommand(pathNodeId),
            cancellationToken);
        if (result.IsSuccess) return NodeCompletionOutcome.Completed;

        return (LearningPathError)result.Error! switch
        {
            LearningPathError.NodeNotFound => NodeCompletionOutcome.NodeNotFound,
            LearningPathError.NodeLocked => NodeCompletionOutcome.NodeLocked,
            LearningPathError.NodeAlreadyCompleted => NodeCompletionOutcome.AlreadyCompleted,
            LearningPathError.OperationCancelled => throw new OperationCanceledException(cancellationToken),
            _ => NodeCompletionOutcome.Failed
        };
    }

    /// <inheritdoc />
    public async Task<bool> HasCompletedSkillAsync(int studentId, string skillTag,
        CancellationToken cancellationToken)
    {
        var completed = await learningPathRepository.FindCompletedSkillTagsByStudentIdAsync(studentId,
            cancellationToken);
        return completed.Contains(skillTag, StringComparer.Ordinal);
    }
}