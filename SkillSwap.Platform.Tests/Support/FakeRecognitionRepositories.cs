using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Aggregates;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Entities;
using SkillSwap.Platform.RecognitionIncentives.Domain.Model.ValueObjects;
using SkillSwap.Platform.RecognitionIncentives.Domain.Repositories;

namespace SkillSwap.Platform.Tests.Support;

public class FakeWalletRepository : IWalletRepository
{
    private readonly List<Wallet> _items = [];
    private int _nextId = 1;

    public IReadOnlyList<Wallet> Items => _items;

    public Task AddAsync(Wallet entity, CancellationToken cancellationToken = default)
    {
        typeof(Wallet).GetProperty(nameof(Wallet.Id))!.SetValue(entity, _nextId++);
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public Task<Wallet?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items.FirstOrDefault(w => w.Id == id));
    }

    public void Update(Wallet entity)
    {
    }

    public void Remove(Wallet entity)
    {
        _items.Remove(entity);
    }

    public Task<IEnumerable<Wallet>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Wallet>>(_items.ToList());
    }

    public Task<Wallet?> FindByOwnerIdAsync(int ownerId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_items.FirstOrDefault(w => w.WalletOwnerId == ownerId));
    }
}

public class FakeCreditTransactionRepository : ICreditTransactionRepository
{
    private readonly List<CreditTransaction> _items = [];
    private int _nextId = 1;

    public IReadOnlyList<CreditTransaction> Items => _items;

    public Task AddAsync(CreditTransaction entity, CancellationToken cancellationToken = default)
    {
        typeof(CreditTransaction).GetProperty(nameof(CreditTransaction.Id))!.SetValue(entity, _nextId++);
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public Task<CreditTransaction?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items.FirstOrDefault(t => t.Id == id));
    }

    public void Update(CreditTransaction entity)
    {
    }

    public void Remove(CreditTransaction entity)
    {
        _items.Remove(entity);
    }

    public Task<IEnumerable<CreditTransaction>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<CreditTransaction>>(_items.ToList());
    }

    public Task<IReadOnlyList<CreditTransaction>> FindByWalletIdAsync(int walletId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CreditTransaction> found = _items
            .Where(t => t.WalletId == walletId)
            .OrderByDescending(t => t.Id)
            .ToList();
        return Task.FromResult(found);
    }

    public Task<bool> ExistsEarnedForCaseAsync(int walletId, int caseId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_items.Any(t => t.WalletId == walletId
                                               && t.Type == TransactionType.Earned
                                               && t.RelatedCaseId == caseId));
    }
}