using SkillSwap.Platform.CredentialVerification.Domain.Model.Aggregates;
using SkillSwap.Platform.CredentialVerification.Domain.Model.Queries;

namespace SkillSwap.Platform.CredentialVerification.Application.QueryServices;

/// <summary>
///     Certificate query service interface
/// </summary>
public interface ICertificateQueryService
{
    Task<Certificate?> Handle(GetCertificateByIdQuery query, CancellationToken cancellationToken);
    Task<IEnumerable<Certificate>> Handle(GetCertificatesByOwnerIdQuery query, CancellationToken cancellationToken);

    /// <summary>
    ///     Builds a temporary signed URL to view the certificate file. The caller must have already
    ///     checked that the requester is the owner or a Coordinator.
    /// </summary>
    string GetFileUrl(Certificate certificate);
}