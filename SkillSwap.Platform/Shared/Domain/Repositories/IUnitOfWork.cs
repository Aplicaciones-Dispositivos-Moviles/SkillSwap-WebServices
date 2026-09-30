namespace SkillSwap.Platform.Shared.Domain.Repositories;

/// <summary>
///     Unit of work interface for all repositories
/// </summary>
public interface IUnitOfWork
{
    Task CompleteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Executes the given operation within a database transaction, committing on success
    ///     and rolling back automatically if an exception is thrown.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}