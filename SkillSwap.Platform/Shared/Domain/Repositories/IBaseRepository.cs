namespace SkillSwap.Platform.Shared.Domain.Repositories;

/// <summary>
///     Base repository for all repositories
/// </summary>
/// <typeparam name="TEntity">The entity type for the repository</typeparam>
public interface IBaseRepository<TEntity>
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity?> FindByIdAsync(int id, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    Task<IEnumerable<TEntity>> ListAsync(CancellationToken cancellationToken = default);
}