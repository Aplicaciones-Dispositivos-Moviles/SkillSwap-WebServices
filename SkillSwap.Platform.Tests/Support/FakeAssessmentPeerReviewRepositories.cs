using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Repositories;

namespace SkillSwap.Platform.Tests.Support;

public class FakeAssessmentAttemptRepository : IAssessmentAttemptRepository
{
    private readonly List<AssessmentAttempt> _attempts = [];
    private int _nextId = 1;

    public IReadOnlyList<AssessmentAttempt> Attempts => _attempts;

    public Task AddAsync(AssessmentAttempt entity, CancellationToken cancellationToken = default)
    {
        typeof(AssessmentAttempt).GetProperty(nameof(AssessmentAttempt.Id))!.SetValue(entity, _nextId++);
        _attempts.Add(entity);
        return Task.CompletedTask;
    }

    public Task<AssessmentAttempt?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_attempts.FirstOrDefault(a => a.Id == id));
    }

    public void Update(AssessmentAttempt entity)
    {
    }

    public void Remove(AssessmentAttempt entity)
    {
        _attempts.Remove(entity);
    }

    public Task<IEnumerable<AssessmentAttempt>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<AssessmentAttempt>>(_attempts.ToList());
    }

    public Task<AssessmentAttempt?> FindByBlueprintIdAsync(int blueprintId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_attempts.FirstOrDefault(a => a.BlueprintId == blueprintId));
    }
}

public class FakeVerifierProfileRepository : IVerifierProfileRepository
{
    private readonly List<VerifierProfile> _profiles = [];
    private int _nextId = 1;

    public IReadOnlyList<VerifierProfile> Profiles => _profiles;

    public Task AddAsync(VerifierProfile entity, CancellationToken cancellationToken = default)
    {
        typeof(VerifierProfile).GetProperty(nameof(VerifierProfile.Id))!.SetValue(entity, _nextId++);
        _profiles.Add(entity);
        return Task.CompletedTask;
    }

    public Task<VerifierProfile?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_profiles.FirstOrDefault(p => p.Id == id));
    }

    public void Update(VerifierProfile entity)
    {
    }

    public void Remove(VerifierProfile entity)
    {
        _profiles.Remove(entity);
    }

    public Task<IEnumerable<VerifierProfile>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<VerifierProfile>>(_profiles.ToList());
    }

    public Task<VerifierProfile?> FindByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_profiles.FirstOrDefault(p => p.VerifierUserId == userId));
    }

    public Task<IReadOnlyList<VerifierProfile>> FindEnabledBySkillTagAsync(string skillTag,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<VerifierProfile> found = _profiles.Where(p => p.CanReview(skillTag)).ToList();
        return Task.FromResult(found);
    }
}

public class FakeVerificationCaseRepository : IVerificationCaseRepository
{
    private readonly List<VerificationCase> _cases = [];
    private int _nextId = 1;

    public IReadOnlyList<VerificationCase> Cases => _cases;

    public Task AddAsync(VerificationCase entity, CancellationToken cancellationToken = default)
    {
        typeof(VerificationCase).GetProperty(nameof(VerificationCase.Id))!.SetValue(entity, _nextId++);
        _cases.Add(entity);
        return Task.CompletedTask;
    }

    public Task<VerificationCase?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_cases.FirstOrDefault(c => c.Id == id));
    }

    public void Update(VerificationCase entity)
    {
    }

    public void Remove(VerificationCase entity)
    {
        _cases.Remove(entity);
    }

    public Task<IEnumerable<VerificationCase>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<VerificationCase>>(_cases.ToList());
    }

    public Task<IReadOnlyList<VerificationCase>> FindByVerifierUserIdAsync(int verifierUserId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<VerificationCase> found = _cases
            .Where(c => c.VerifierUserId == verifierUserId)
            .OrderByDescending(c => c.Id)
            .ToList();
        return Task.FromResult(found);
    }

    public Task<VerificationCase?> FindOpenByStudentAndNodeAsync(int studentId, int pathNodeId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_cases.FirstOrDefault(c =>
            c.StudentId == studentId && c.PathNodeId == pathNodeId && c.IsOpen));
    }

    public Task<IReadOnlyList<VerificationCase>> FindPendingBySkillTagAsync(string skillTag,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<VerificationCase> found = _cases
            .Where(c => c.Status == CaseStatus.Pending && c.SkillTag == skillTag)
            .OrderBy(c => c.Id)
            .ToList();
        return Task.FromResult(found);
    }

    public Task<IReadOnlyDictionary<int, int>> CountOpenByVerifierUserIdsAsync(
        IReadOnlyCollection<int> verifierUserIds, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<int, int> counts = verifierUserIds.Distinct().ToDictionary(
            id => id,
            id => _cases.Count(c => c.VerifierUserId == id && c.IsOpen));
        return Task.FromResult(counts);
    }
}