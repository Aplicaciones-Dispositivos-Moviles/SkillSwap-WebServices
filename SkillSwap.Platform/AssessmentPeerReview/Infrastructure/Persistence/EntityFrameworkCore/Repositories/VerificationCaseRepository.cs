using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Repositories;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace SkillSwap.Platform.AssessmentPeerReview.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Verification case repository implementation over the "verification_cases" table
/// </summary>
/// <param name="context">The EF Core database context</param>
public class VerificationCaseRepository(AppDbContext context)
    : BaseRepository<VerificationCase>(context), IVerificationCaseRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<VerificationCase>> FindByVerifierUserIdAsync(int verifierUserId,
        CancellationToken cancellationToken)
    {
        return await Context.Set<VerificationCase>()
            .Where(c => c.VerifierUserId == verifierUserId)
            .OrderByDescending(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<VerificationCase?> FindOpenByStudentAndNodeAsync(int studentId, int pathNodeId,
        CancellationToken cancellationToken)
    {
        return await Context.Set<VerificationCase>()
            .FirstOrDefaultAsync(c => c.StudentId == studentId
                                      && c.PathNodeId == pathNodeId
                                      && c.Status != CaseStatus.Resolved, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VerificationCase>> FindPendingBySkillTagAsync(string skillTag,
        CancellationToken cancellationToken)
    {
        return await Context.Set<VerificationCase>()
            .Where(c => c.Status == CaseStatus.Pending && c.SkillTag == skillTag)
            .OrderBy(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, int>> CountOpenByVerifierUserIdsAsync(
        IReadOnlyCollection<int> verifierUserIds, CancellationToken cancellationToken)
    {
        var ids = verifierUserIds.Distinct().ToList();
        var counts = await Context.Set<VerificationCase>()
            .Where(c => c.VerifierUserId != null && ids.Contains(c.VerifierUserId.Value)
                                                 && c.Status != CaseStatus.Resolved)
            .GroupBy(c => c.VerifierUserId!.Value)
            .Select(group => new { UserId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return ids.ToDictionary(id => id, id => counts.FirstOrDefault(c => c.UserId == id)?.Count ?? 0);
    }
}