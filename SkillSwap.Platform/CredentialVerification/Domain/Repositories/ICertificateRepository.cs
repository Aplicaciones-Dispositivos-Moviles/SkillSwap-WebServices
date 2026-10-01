using SkillSwap.Platform.CredentialVerification.Domain.Model.Aggregates;
using SkillSwap.Platform.Shared.Domain.Repositories;

namespace SkillSwap.Platform.CredentialVerification.Domain.Repositories;

/// <summary>
///     Certificate repository interface, including the queries needed to detect duplicates
/// </summary>
public interface ICertificateRepository : IBaseRepository<Certificate>
{
    Task<IEnumerable<Certificate>> FindByOwnerIdAsync(int ownerId, CancellationToken cancellationToken);

    /// <summary>
    ///     The certificate this owner already registered with this file hash, if any.
    /// </summary>
    Task<Certificate?> FindByFileHashAsync(int ownerId, string fileHash, CancellationToken cancellationToken);

    /// <summary>
    ///     Whether a different user already registered this certificate number.
    /// </summary>
    Task<bool> ExistsByCertificateNumberExcludingOwnerAsync(int ownerId, string certificateNumber,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Whether a different user already registered this verification code.
    /// </summary>
    Task<bool> ExistsByVerificationCodeExcludingOwnerAsync(int ownerId, string verificationCode,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Whether a different user already uploaded a file with this hash.
    /// </summary>
    Task<bool> ExistsByFileHashExcludingOwnerAsync(int ownerId, string fileHash,
        CancellationToken cancellationToken);
}