using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Reputation.Domain.Repositories;

namespace SkillSwap.Platform.Tests.Support;

public class FakeVerifierReliabilityRepository : IVerifierReliabilityRepository
{
    private readonly List<VerifierReliability> _items = [];
    private int _nextId = 1;

    public IReadOnlyList<VerifierReliability> Items => _items;

    public Task AddAsync(VerifierReliability entity, CancellationToken cancellationToken = default)
    {
        typeof(VerifierReliability).GetProperty(nameof(VerifierReliability.Id))!.SetValue(entity, _nextId++);
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public Task<VerifierReliability?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items.FirstOrDefault(r => r.Id == id));
    }

    public void Update(VerifierReliability entity)
    {
    }

    public void Remove(VerifierReliability entity)
    {
        _items.Remove(entity);
    }

    public Task<IEnumerable<VerifierReliability>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<VerifierReliability>>(_items.ToList());
    }

    public Task<VerifierReliability?> FindByVerifierUserIdAsync(int verifierUserId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_items.FirstOrDefault(r => r.VerifierUserId == verifierUserId));
    }
}

public class FakeStudentEmployabilityScoreRepository : IStudentEmployabilityScoreRepository
{
    private readonly List<StudentEmployabilityScore> _items = [];
    private int _nextId = 1;

    public IReadOnlyList<StudentEmployabilityScore> Items => _items;

    public Task AddAsync(StudentEmployabilityScore entity, CancellationToken cancellationToken = default)
    {
        typeof(StudentEmployabilityScore).GetProperty(nameof(StudentEmployabilityScore.Id))!
            .SetValue(entity, _nextId++);
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public Task<StudentEmployabilityScore?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items.FirstOrDefault(s => s.Id == id));
    }

    public void Update(StudentEmployabilityScore entity)
    {
    }

    public void Remove(StudentEmployabilityScore entity)
    {
        _items.Remove(entity);
    }

    public Task<IEnumerable<StudentEmployabilityScore>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<StudentEmployabilityScore>>(_items.ToList());
    }

    public Task<StudentEmployabilityScore?> FindByStudentIdAsync(int studentId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_items.FirstOrDefault(s => s.StudentId == studentId));
    }
}