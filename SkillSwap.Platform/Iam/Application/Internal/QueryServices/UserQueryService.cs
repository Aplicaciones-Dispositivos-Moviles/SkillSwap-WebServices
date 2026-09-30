using SkillSwap.Platform.Iam.Application.QueryServices;
using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Domain.Model.Queries;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Domain.Repositories;

namespace SkillSwap.Platform.Iam.Application.Internal.QueryServices;

/// <summary>
///     User query service
/// </summary>
/// <param name="userRepository">User repository</param>
public class UserQueryService(IUserRepository userRepository) : IUserQueryService
{
    /// <inheritdoc />
    public async Task<User?> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        return await userRepository.FindByIdAsync(query.UserId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> Handle(GetUserByUsernameQuery query, CancellationToken cancellationToken)
    {
        return Username.IsValid(query.Username)
            ? await userRepository.FindByUsernameAsync(new Username(query.Username), cancellationToken)
            : null;
    }
}