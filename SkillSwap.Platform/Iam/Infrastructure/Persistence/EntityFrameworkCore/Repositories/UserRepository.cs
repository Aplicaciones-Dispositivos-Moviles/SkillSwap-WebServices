using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.Iam.Domain.Model.Aggregates;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Domain.Repositories;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace SkillSwap.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     User repository implementation over the "users" table
/// </summary>
/// <param name="context">The EF Core database context</param>
public class UserRepository(AppDbContext context) : BaseRepository<User>(context), IUserRepository
{
    /// <inheritdoc />
    public async Task<User?> FindByUsernameAsync(Username username, CancellationToken cancellationToken)
    {
        return await Context.Set<User>().FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken)
    {
        return await Context.Set<User>().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByUsernameAsync(Username username, CancellationToken cancellationToken)
    {
        return await Context.Set<User>().AnyAsync(u => u.Username == username, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken)
    {
        return await Context.Set<User>().AnyAsync(u => u.Email == email, cancellationToken);
    }
}