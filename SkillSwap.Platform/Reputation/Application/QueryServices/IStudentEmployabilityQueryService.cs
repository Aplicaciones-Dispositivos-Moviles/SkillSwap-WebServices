using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Reputation.Domain.Model.Queries;

namespace SkillSwap.Platform.Reputation.Application.QueryServices;

public interface IStudentEmployabilityQueryService
{
    Task<StudentEmployabilityScore?> Handle(GetStudentEmployabilityByStudentIdQuery query,
        CancellationToken cancellationToken);
}