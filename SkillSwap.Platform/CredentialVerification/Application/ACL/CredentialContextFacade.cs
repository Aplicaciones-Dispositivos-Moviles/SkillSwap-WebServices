using SkillSwap.Platform.CredentialVerification.Domain.Model.ValueObjects;
using SkillSwap.Platform.CredentialVerification.Domain.Repositories;

namespace SkillSwap.Platform.CredentialVerification.Application.ACL;

/// <summary>
///     Facade implementation over the certificate repository
/// </summary>
/// <param name="certificateRepository">Certificate repository</param>
public class CredentialContextFacade(ICertificateRepository certificateRepository) : ICredentialContextFacade
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CertificateSummary>> GetEvidenceCertificatesAsync(int ownerId,
        CancellationToken cancellationToken)
    {
        var certificates = await certificateRepository.FindByOwnerIdAsync(ownerId, cancellationToken);

        return certificates
            .Where(c => c.Status is VerificationStatus.Unverified or VerificationStatus.Verified)
            .OrderBy(c => c.Id)
            .Select(c => new CertificateSummary(c.Id, c.CourseName, c.InstitutionName))
            .ToList();
    }
}