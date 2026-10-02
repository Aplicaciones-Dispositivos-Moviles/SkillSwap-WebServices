using SkillSwap.Platform.RecognitionIncentives.Domain.Model.Aggregates;
using SkillSwap.Platform.Shared.Domain.Repositories;

namespace SkillSwap.Platform.RecognitionIncentives.Domain.Repositories;

public interface IWalletRepository : IBaseRepository<Wallet>
{
    Task<Wallet?> FindByOwnerIdAsync(int ownerId, CancellationToken cancellationToken);
}