namespace SkillSwap.Platform.CredentialVerification.Application.ACL;

/// <summary>
///     Minimal view of a certificate that other bounded contexts may consume.
/// </summary>
public sealed record CertificateSummary(int Id, string? CourseName, string? InstitutionName);

/// <summary>
///     Anti-corruption facade through which other bounded contexts read Credential Verification data,
///     without depending on its aggregates or repositories.
/// </summary>
public interface ICredentialContextFacade
{
    /// <summary>
    ///     The certificates of a student that can support a skill as evidence: those that are neither
    ///     suspicious nor rejected. Ordered by id, so the oldest certificate comes first.
    /// </summary>
    Task<IReadOnlyList<CertificateSummary>> GetEvidenceCertificatesAsync(int ownerId,
        CancellationToken cancellationToken);
}