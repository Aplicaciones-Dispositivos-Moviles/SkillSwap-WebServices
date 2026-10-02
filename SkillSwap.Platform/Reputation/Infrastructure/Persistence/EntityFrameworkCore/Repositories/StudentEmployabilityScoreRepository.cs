using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Reputation.Domain.Repositories;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace SkillSwap.Platform.Reputation.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Student employability score repository implementation over the "student_employability_scores" table
/// </summary>
/// <param name="context">The EF Core database context</param>
public class StudentEmployabilityScoreRepository(AppDbContext context)
    : BaseRepository<StudentEmployabilityScore>(context), IStudentEmployabilityScoreRepository
{
    /// <inheritdoc />
    public async Task<StudentEmployabilityScore?> FindByStudentIdAsync(int studentId,
        CancellationToken cancellationToken)
    {
        return await Context.Set<StudentEmployabilityScore>()
            .FirstOrDefaultAsync(s => s.StudentId == studentId, cancellationToken);
    }
}