namespace SkillSwap.Platform.LearningPathEngine.Domain.Model.Queries;

/// <summary>
///     Get the latest assessment blueprint generated for a path node
/// </summary>
/// <param name="PathNodeId">The node whose blueprint is requested</param>
public record GetAssessmentBlueprintByPathNodeIdQuery(int PathNodeId);