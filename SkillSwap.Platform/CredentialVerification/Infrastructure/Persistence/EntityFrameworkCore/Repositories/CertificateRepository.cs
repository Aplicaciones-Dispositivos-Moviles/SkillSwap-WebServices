using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.CredentialVerification.Domain.Model.Aggregates;
using SkillSwap.Platform.CredentialVerification.Domain.Repositories;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using SkillSwap.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

namespace SkillSwap.Platform.CredentialVerification.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Certificate repository implementation over the "certificates" table
/// </summary>
/// <param name="context">The EF Core database context</param>
public class CertificateRepository(AppDbContext context)
    : BaseRepository<Certificate>(context), ICertificateRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<Certificate>> FindByOwnerIdAsync(int ownerId, CancellationToken cancellationToken)
    {
        return await Context.Set<Certificate>()
            .Where(c => c.OwnerId == ownerId)
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByFileHashAsync(int ownerId, string fileHash, CancellationToken cancellationToken)
    {
        return await Context.Set<Certificate>()
            .AnyAsync(c => c.OwnerId == ownerId && c.FileHash == fileHash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByCertificateNumberExcludingOwnerAsync(int ownerId, string certificateNumber,
        CancellationToken cancellationToken)
    {
        return await Context.Set<Certificate>()
            .AnyAsync(c => c.OwnerId != ownerId && c.CertificateNumber == certificateNumber, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByVerificationCodeExcludingOwnerAsync(int ownerId, string verificationCode,
        CancellationToken cancellationToken)
    {
        return await Context.Set<Certificate>()
            .AnyAsync(c => c.OwnerId != ownerId && c.VerificationCode == verificationCode, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByFileHashExcludingOwnerAsync(int ownerId, string fileHash,
        CancellationToken cancellationToken)
    {
        return await Context.Set<Certificate>()
            .AnyAsync(c => c.OwnerId != ownerId && c.FileHash == fileHash, cancellationToken);
    }
}