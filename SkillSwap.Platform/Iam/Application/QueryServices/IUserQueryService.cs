using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Domain.Model.Queries;

namespace SkillSwap.Platform.Iam.Application.QueryServices;

/// <summary>
///     User query service interface
/// </summary>
public interface IUserQueryService
{
    Task<User?> Handle(GetUserByIdQuery query, CancellationToken cancellationToken);
    Task<User?> Handle(GetUserByUsernameQuery query, CancellationToken cancellationToken);
}