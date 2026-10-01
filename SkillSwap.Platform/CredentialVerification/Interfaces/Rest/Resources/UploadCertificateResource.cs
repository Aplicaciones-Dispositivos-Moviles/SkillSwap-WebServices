namespace SkillSwap.Platform.CredentialVerification.Interfaces.Rest.Resources;

/// <summary>
///     Multipart form sent by the mobile app: the certificate file plus the fields the on-device OCR
///     managed to read. Everything except the file is optional. The owner is never part of the form:
///     it is always taken from the authenticated user.
/// </summary>
/// <param name="File">The certificate file (JPG, PNG or PDF, up to 10 MB)</param>
/// <param name="HolderName">Holder name read by the OCR</param>
/// <param name="InstitutionName">Issuing institution read by the OCR</param>
/// <param name="CourseName">Course or program name read by the OCR</param>
/// <param name="IssueDate">Issue date read by the OCR (yyyy-MM-dd)</param>
/// <param name="DurationHours">Duration in hours read by the OCR</param>
/// <param name="CertificateNumber">Certificate number read by the OCR</param>
/// <param name="VerificationCode">Verification code read by the OCR</param>
/// <param name="VerificationUrl">Official verification URL read by the OCR</param>
/// <param name="QrPayload">Decoded content of the QR code</param>
/// <param name="OcrText">Full text recognized by the OCR</param>
public record UploadCertificateResource(
    IFormFile? File,
    string? HolderName,
    string? InstitutionName,
    string? CourseName,
    DateOnly? IssueDate,
    int? DurationHours,
    string? CertificateNumber,
    string? VerificationCode,
    string? VerificationUrl,
    string? QrPayload,
    string? OcrText);