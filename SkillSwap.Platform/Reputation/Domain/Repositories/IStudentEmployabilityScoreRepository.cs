using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Shared.Domain.Repositories;

namespace SkillSwap.Platform.Reputation.Domain.Repositories;

public interface IStudentEmployabilityScoreRepository : IBaseRepository<StudentEmployabilityScore>
{
    Task<StudentEmployabilityScore?> FindByStudentIdAsync(int studentId, CancellationToken cancellationToken);
}