using SkillSwap.Platform.CredentialVerification.Application.Internal.OutboundServices;
using SkillSwap.Platform.CredentialVerification.Application.QueryServices;
using SkillSwap.Platform.CredentialVerification.Domain.Model.Aggregates;
using SkillSwap.Platform.CredentialVerification.Domain.Model.Queries;
using SkillSwap.Platform.CredentialVerification.Domain.Repositories;

namespace SkillSwap.Platform.CredentialVerification.Application.Internal.QueryServices;

/// <summary>
///     Certificate query service
/// </summary>
/// <param name="certificateRepository">Certificate repository</param>
/// <param name="fileStorageService">File storage port</param>
public class CertificateQueryService(
    ICertificateRepository certificateRepository,
    IFileStorageService fileStorageService)
    : ICertificateQueryService
{
    public static readonly TimeSpan FileUrlLifetime = TimeSpan.FromMinutes(15);

    /// <inheritdoc />
    public async Task<Certificate?> Handle(GetCertificateByIdQuery query, CancellationToken cancellationToken)
    {
        return await certificateRepository.FindByIdAsync(query.CertificateId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Certificate>> Handle(GetCertificatesByOwnerIdQuery query,
        CancellationToken cancellationToken)
    {
        return await certificateRepository.FindByOwnerIdAsync(query.OwnerId, cancellationToken);
    }

    /// <inheritdoc />
    public string GetFileUrl(Certificate certificate)
    {
        return fileStorageService.GetTemporaryUrl(certificate.StorageReference, FileUrlLifetime);
    }
}