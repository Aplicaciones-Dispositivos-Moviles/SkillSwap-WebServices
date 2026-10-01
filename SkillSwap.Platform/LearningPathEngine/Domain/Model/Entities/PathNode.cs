using SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.LearningPathEngine.Domain.Model.Entities;

/// <summary>
///     One step of a learning path: a skill the student has to demonstrate.
/// </summary>
/// <remarks>
///     Its state is changed only through the <see cref="Aggregates.LearningPath" /> aggregate root.
/// </remarks>
public class PathNode
{
    /// <summary>
    ///     Parameterless constructor required by EF Core.
    /// </summary>
    protected PathNode()
    {
        SkillTag = null!;
        PrerequisiteSkillTags = [];
    }

    public PathNode(string skillTag, int order, IEnumerable<string> prerequisiteSkillTags)
    {
        if (string.IsNullOrWhiteSpace(skillTag))
            throw new DomainException("The skill tag cannot be empty.");
        if (order <= 0)
            throw new DomainException("The node order must be positive.");

        SkillTag = skillTag.Trim();
        Order = order;
        PrerequisiteSkillTags = prerequisiteSkillTags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(tag => tag, StringComparer.Ordinal)
            .ToList();
        if (PrerequisiteSkillTags.Contains(SkillTag))
            throw new DomainException("A skill cannot be a prerequisite of itself.");

        Status = PrerequisiteSkillTags.Count == 0 ? NodeStatus.Available : NodeStatus.Locked;
    }

    public int Id { get; private set; }
    public string SkillTag { get; private set; }
    public int Order { get; private set; }
    public NodeStatus Status { get; private set; }

    /// <summary>
    ///     Skills of this same path that must be completed before this node becomes available.
    /// </summary>
    public IReadOnlyList<string> PrerequisiteSkillTags { get; private set; }

    /// <summary>
    ///     Certificate that supports this skill as evidence. It never completes the node.
    /// </summary>
    public int? LinkedCertificateId { get; private set; }

    /// <summary>
    ///     Latest assessment generated for this node.
    /// </summary>
    public int? AssessmentBlueprintId { get; private set; }

    internal void Unlock()
    {
        if (Status == NodeStatus.Locked) Status = NodeStatus.Available;
    }

    internal void Complete()
    {
        Status = NodeStatus.Completed;
    }

    internal void LinkCertificate(int certificateId)
    {
        LinkedCertificateId = certificateId;
    }

    internal void AttachBlueprint(int blueprintId)
    {
        AssessmentBlueprintId = blueprintId;
    }
}