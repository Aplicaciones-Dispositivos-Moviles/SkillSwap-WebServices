using SkillSwap.Platform.Reputation.Application.QueryServices;
using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Reputation.Domain.Model.Queries;
using SkillSwap.Platform.Reputation.Domain.Repositories;

namespace SkillSwap.Platform.Reputation.Application.Internal.QueryServices;

/// <summary>
///     Student employability query service
/// </summary>
/// <param name="employabilityRepository">Student employability score repository</param>
public class StudentEmployabilityQueryService(IStudentEmployabilityScoreRepository employabilityRepository)
    : IStudentEmployabilityQueryService
{
    /// <inheritdoc />
    public async Task<StudentEmployabilityScore?> Handle(GetStudentEmployabilityByStudentIdQuery query,
        CancellationToken cancellationToken)
    {
        return await employabilityRepository.FindByStudentIdAsync(query.StudentId, cancellationToken);
    }
}