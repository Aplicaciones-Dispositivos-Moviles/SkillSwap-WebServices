namespace SkillSwap.Platform.LearningPathEngine.Interfaces.Rest.Resources;

/// <summary>
///     One step of a learning path: a skill the student has to demonstrate
/// </summary>
/// <param name="Id">The unique identifier of the node</param>
/// <param name="SkillTag">The skill code in the taxonomy</param>
/// <param name="SkillName">The display name of the skill</param>
/// <param name="Order">Position of the node in the path (1 = first)</param>
/// <param name="Status">Locked, Available or Completed</param>
/// <param name="PrerequisiteSkillTags">Skills of this path that must be completed first</param>
/// <param name="LinkedCertificateId">Certificate linked as supporting evidence, if any. It never completes the node.</param>
/// <param name="AssessmentBlueprintId">Latest assessment generated for the node, if any</param>
public record PathNodeResource(
    int Id,
    string SkillTag,
    string SkillName,
    int Order,
    string Status,
    IReadOnlyList<string> PrerequisiteSkillTags,
    int? LinkedCertificateId,
    int? AssessmentBlueprintId);