using SkillSwap.Platform.LearningPathEngine.Domain.Model.Entities;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;

/// <summary>
///     LearningPath aggregate root
/// </summary>
/// <remarks>
///     The personalized path of a student: an ordered set of nodes (skills to demonstrate) toward the
///     declared goal. A node unlocks when all its prerequisites are completed. A certificate can be
///     linked to a node as evidence, but only an approved assessment completes it.
/// </remarks>
public class LearningPath
{
    private readonly List<PathNode> _nodes = [];

    /// <summary>
    ///     Parameterless constructor required by EF Core.
    /// </summary>
    protected LearningPath()
    {
        CareerGoal = null!;
    }

    public LearningPath(int studentId, CareerGoal careerGoal, IEnumerable<PathNode> nodes)
    {
        if (studentId <= 0)
            throw new DomainException("The path must belong to a valid student.");

        var list = nodes.OrderBy(node => node.Order).ToList();
        if (list.Count == 0)
            throw new DomainException("A learning path needs at least one node.");
        if (list.Select(node => node.Order).Distinct().Count() != list.Count)
            throw new DomainException("The node orders of a path must be unique.");

        var tags = list.Select(node => node.SkillTag).ToHashSet(StringComparer.Ordinal);
        if (tags.Count != list.Count)
            throw new DomainException("A skill can appear only once in a path.");
        if (list.Any(node => node.PrerequisiteSkillTags.Any(prerequisite => !tags.Contains(prerequisite))))
            throw new DomainException("A node references a prerequisite that is not part of the path.");
        if (list.Any(node => node.Status == NodeStatus.Completed))
            throw new DomainException("A new learning path cannot contain completed nodes.");

        StudentId = studentId;
        CareerGoal = careerGoal;
        _nodes.AddRange(list);
        Status = PathStatus.Active;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public int Id { get; private set; }
    public int StudentId { get; private set; }
    public CareerGoal CareerGoal { get; private set; }

    /// <summary>
    ///     The nodes, in path order.
    /// </summary>
    public IReadOnlyList<PathNode> Nodes => _nodes.OrderBy(node => node.Order).ToList();

    public PathStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    ///     Finds a node of this path, or null when it does not belong to it.
    /// </summary>
    public PathNode? GetNode(int nodeId)
    {
        return _nodes.FirstOrDefault(node => node.Id == nodeId);
    }

    /// <summary>
    ///     Marks an available node as completed and unlocks the nodes whose prerequisites are now all
    ///     completed. When the last node is completed, the whole path is completed.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the node is unknown, locked or already completed.</exception>
    public LearningPath CompleteNode(int nodeId)
    {
        var node = RequireNode(nodeId);
        if (node.Status == NodeStatus.Locked)
            throw new DomainException($"The node '{node.SkillTag}' is locked: complete its prerequisites first.");
        if (node.Status == NodeStatus.Completed)
            throw new DomainException($"The node '{node.SkillTag}' is already completed.");

        node.Complete();

        var completed = _nodes.Where(n => n.Status == NodeStatus.Completed)
            .Select(n => n.SkillTag).ToHashSet(StringComparer.Ordinal);
        var unlockable = _nodes
            .Where(n => n.Status == NodeStatus.Locked && n.PrerequisiteSkillTags.All(completed.Contains))
            .ToList();
        foreach (var locked in unlockable) locked.Unlock();

        if (_nodes.All(n => n.Status == NodeStatus.Completed)) Status = PathStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
        return this;
    }

    /// <summary>
    ///     Links a certificate to a node as supporting evidence. The first certificate linked to a node
    ///     is kept, and completed nodes accept none.
    /// </summary>
    /// <returns>True when the certificate was linked; false when the node already had one or is completed.</returns>
    /// <exception cref="DomainException">Thrown when the node is unknown or the certificate id is invalid.</exception>
    public bool LinkCertificate(int nodeId, int certificateId)
    {
        if (certificateId <= 0)
            throw new DomainException("The certificate id is not valid.");

        var node = RequireNode(nodeId);
        if (node.Status == NodeStatus.Completed || node.LinkedCertificateId is not null) return false;

        node.LinkCertificate(certificateId);
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    ///     Points an available node to its latest generated assessment. A new attempt replaces the
    ///     previous pointer (older blueprints remain as history).
    /// </summary>
    /// <exception cref="DomainException">Thrown when the node is unknown or not available.</exception>
    public LearningPath AttachBlueprint(int nodeId, int blueprintId)
    {
        if (blueprintId <= 0)
            throw new DomainException("The blueprint id is not valid.");

        var node = RequireNode(nodeId);
        if (node.Status != NodeStatus.Available)
            throw new DomainException("An assessment can only be attached to an available node.");

        node.AttachBlueprint(blueprintId);
        UpdatedAt = DateTime.UtcNow;
        return this;
    }

    private PathNode RequireNode(int nodeId)
    {
        return GetNode(nodeId) ?? throw new DomainException($"The node {nodeId} does not belong to this path.");
    }
}