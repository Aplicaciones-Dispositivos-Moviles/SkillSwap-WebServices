using SkillSwap.Platform.Shared.Domain.Repositories;

namespace SkillSwap.Platform.Tests.Support;

public class FakeUnitOfWork : IUnitOfWork
{
    /// <summary>
    ///     When set, <see cref="CompleteAsync" /> throws it, simulating a persistence failure.
    /// </summary>
    public Exception? ExceptionToThrow { get; set; }

    public int CompleteCalls { get; private set; }

    public Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        CompleteCalls++;
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.CompletedTask;
    }

    public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        return operation();
    }
}