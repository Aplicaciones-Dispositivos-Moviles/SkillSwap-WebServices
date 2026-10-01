using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Repositories;

namespace SkillSwap.Platform.Tests.Support;

public class FakeAssessmentBlueprintRepository : IAssessmentBlueprintRepository
{
    private readonly List<AssessmentBlueprint> _blueprints = [];
    private int _nextId = 1;

    public IReadOnlyList<AssessmentBlueprint> Blueprints => _blueprints;

    public Task AddAsync(AssessmentBlueprint entity, CancellationToken cancellationToken = default)
    {
        typeof(AssessmentBlueprint).GetProperty(nameof(AssessmentBlueprint.Id))!.SetValue(entity, _nextId++);
        _blueprints.Add(entity);
        return Task.CompletedTask;
    }

    public Task<AssessmentBlueprint?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_blueprints.FirstOrDefault(b => b.Id == id));
    }

    public void Update(AssessmentBlueprint entity)
    {
    }

    public void Remove(AssessmentBlueprint entity)
    {
        _blueprints.Remove(entity);
    }

    public Task<IEnumerable<AssessmentBlueprint>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<AssessmentBlueprint>>(_blueprints.ToList());
    }

    public Task<AssessmentBlueprint?> FindLatestByPathNodeIdAsync(int pathNodeId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_blueprints.Where(b => b.PathNodeId == pathNodeId).OrderByDescending(b => b.Id)
            .FirstOrDefault());
    }
}