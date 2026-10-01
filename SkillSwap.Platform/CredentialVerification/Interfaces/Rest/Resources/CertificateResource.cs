namespace SkillSwap.Platform.CredentialVerification.Interfaces.Rest.Resources;

/// <summary>
///     Certificate resource for REST API
/// </summary>
/// <param name="Id">The unique identifier of the certificate</param>
/// <param name="OwnerId">The student who owns the certificate</param>
/// <param name="HolderName">Holder name</param>
/// <param name="InstitutionName">Issuing institution</param>
/// <param name="CourseName">Course or program name</param>
/// <param name="IssueDate">Issue date</param>
/// <param name="DurationHours">Duration in hours</param>
/// <param name="CertificateNumber">Certificate number</param>
/// <param name="VerificationCode">Verification code</param>
/// <param name="VerificationUrl">Official verification URL</param>
/// <param name="Status">Unverified, Suspicious, Verified or Rejected</param>
/// <param name="VerificationMethod">OcrOnly or Manual in the implemented scope</param>
/// <param name="RiskLevel">LowRisk, Review or HighRisk. The numeric score stays internal.</param>
/// <param name="CreatedAt">When the certificate was registered (UTC)</param>
/// <param name="VerifiedAt">When a Coordinator resolved it (UTC), if ever</param>
/// <param name="FileUrl">Temporary signed URL to view the file (expires in 15 minutes)</param>
public record CertificateResource(
    int Id,
    int OwnerId,
    string? HolderName,
    string? InstitutionName,
    string? CourseName,
    DateOnly? IssueDate,
    int? DurationHours,
    string? CertificateNumber,
    string? VerificationCode,
    string? VerificationUrl,
    string Status,
    string VerificationMethod,
    string? RiskLevel,
    DateTime CreatedAt,
    DateTime? VerifiedAt,
    string FileUrl);