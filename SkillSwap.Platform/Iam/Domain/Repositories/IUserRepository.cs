using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Repositories;

namespace SkillSwap.Platform.Iam.Domain.Repositories;

/// <summary>
///     User repository interface
/// </summary>
public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> FindByUsernameAsync(Username username, CancellationToken cancellationToken);
    Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken);
    Task<bool> ExistsByUsernameAsync(Username username, CancellationToken cancellationToken);
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken);
}