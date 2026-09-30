namespace SkillSwap.Platform.CredentialVerification.Domain.Model.Queries;

/// <summary>
///     Get certificate by id query
/// </summary>
/// <param name="CertificateId">The unique identifier of the certificate</param>
public record GetCertificateByIdQuery(int CertificateId);