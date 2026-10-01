namespace SkillSwap.Platform.LearningPathEngine.Application.ACL;

/// <summary>
///     A question of a blueprint, as seen by other bounded contexts. It includes the correct answer, so
///     it must never be mapped to a resource.
/// </summary>
public sealed record BlueprintQuestionView(string Text, IReadOnlyList<string> Answers, int CorrectAnswer);

/// <summary>
///     Minimal view of an assessment blueprint that other bounded contexts may consume to grade an attempt.
/// </summary>
/// <param name="BlueprintId">The blueprint</param>
/// <param name="PathNodeId">The node it assesses</param>
/// <param name="StudentId">The owner of the path the node belongs to</param>
/// <param name="SkillTag">The skill demonstrated by the node</param>
/// <param name="IsLatest">Whether it is the latest blueprint generated for the node</param>
/// <param name="NodeIsAvailable">Whether the node can still be demonstrated (neither locked nor completed)</param>
/// <param name="Questions">The questions in order, with their correct answers</param>
public sealed record BlueprintView(
    int BlueprintId,
    int PathNodeId,
    int StudentId,
    string SkillTag,
    bool IsLatest,
    bool NodeIsAvailable,
    IReadOnlyList<BlueprintQuestionView> Questions);

/// <summary>
///     Outcome of asking Learning Path Engine to complete a node.
/// </summary>
public enum NodeCompletionOutcome
{
    Completed,
    NodeNotFound,
    NodeLocked,
    AlreadyCompleted,
    Failed
}

/// <summary>
///     Anti-corruption facade through which other bounded contexts use Learning Path Engine, without
///     depending on its aggregates or repositories.
/// </summary>
public interface ILearningPathContextFacade
{
    /// <summary>
    ///     The blueprint with the data needed to grade an attempt, or null when it does not exist.
    /// </summary>
    Task<BlueprintView?> GetBlueprintAsync(int blueprintId, CancellationToken cancellationToken);

    /// <summary>
    ///     Completes the node, unlocking the ones that depended on it. A persistence failure is reported as
    ///     <see cref="NodeCompletionOutcome.Failed" />; a cancelled request throws.
    /// </summary>
    Task<NodeCompletionOutcome> CompleteNodeAsync(int pathNodeId, CancellationToken cancellationToken);

    /// <summary>
    ///     Whether the student has a completed node for the skill in any of their paths.
    /// </summary>
    Task<bool> HasCompletedSkillAsync(int studentId, string skillTag, CancellationToken cancellationToken);
}