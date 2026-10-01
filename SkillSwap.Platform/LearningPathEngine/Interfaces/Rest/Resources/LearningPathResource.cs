namespace SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Resources;

/// <summary>
///     Learning path resource for REST API
/// </summary>
/// <param name="Id">The unique identifier of the path</param>
/// <param name="StudentId">The student who owns the path</param>
/// <param name="Goal">The goal as the student wrote it</param>
/// <param name="GoalSkillTags">The skills of the taxonomy the goal was interpreted as</param>
/// <param name="Status">Active or Completed</param>
/// <param name="Nodes">The nodes, in prerequisite order</param>
/// <param name="CreatedAt">When the path was created (UTC)</param>
/// <param name="UpdatedAt">When the path last changed (UTC)</param>
public record LearningPathResource(
    int Id,
    int StudentId,
    string Goal,
    IReadOnlyList<string> GoalSkillTags,
    string Status,
    IReadOnlyList<PathNodeResource> Nodes,
    DateTime CreatedAt,
    DateTime UpdatedAt);