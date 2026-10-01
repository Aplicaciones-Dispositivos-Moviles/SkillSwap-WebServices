using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Entities;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;
using SkillSwap.Platform.LearningPathEngine.Domain.Repositories;

namespace SkillSwap.Platform.Tests.Support;

public class FakeLearningPathRepository : ILearningPathRepository
{
    private readonly List<LearningPath> _paths = [];
    private int _nextId = 1;
    private int _nextNodeId = 1;

    public IReadOnlyList<LearningPath> Paths => _paths;

    public Task AddAsync(LearningPath entity, CancellationToken cancellationToken = default)
    {
        typeof(LearningPath).GetProperty(nameof(LearningPath.Id))!.SetValue(entity, _nextId++);
        foreach (var node in entity.Nodes)
            typeof(PathNode).GetProperty(nameof(PathNode.Id))!.SetValue(node, _nextNodeId++);
        _paths.Add(entity);
        return Task.CompletedTask;
    }

    public Task<LearningPath?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_paths.FirstOrDefault(p => p.Id == id));
    }

    public void Update(LearningPath entity)
    {
    }

    public void Remove(LearningPath entity)
    {
        _paths.Remove(entity);
    }

    public Task<IEnumerable<LearningPath>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<LearningPath>>(_paths.ToList());
    }

    public Task<LearningPath?> FindLatestByStudentIdAsync(int studentId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_paths.Where(p => p.StudentId == studentId).OrderByDescending(p => p.Id)
            .FirstOrDefault());
    }

    public Task<LearningPath?> FindByNodeIdAsync(int nodeId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_paths.FirstOrDefault(p => p.Nodes.Any(n => n.Id == nodeId)));
    }

    public Task<IReadOnlyCollection<string>> FindCompletedSkillTagsByStudentIdAsync(int studentId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> tags = _paths
            .Where(p => p.StudentId == studentId)
            .SelectMany(p => p.Nodes)
            .Where(n => n.Status == NodeStatus.Completed)
            .Select(n => n.SkillTag)
            .Distinct()
            .ToList();
        return Task.FromResult(tags);
    }
}