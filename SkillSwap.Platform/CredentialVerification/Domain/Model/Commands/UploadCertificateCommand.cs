namespace SkillSwap.Platform.CredentialVerification.Domain.Model.Commands;

/// <summary>
///     Upload certificate command
/// </summary>
/// <param name="OwnerId">The authenticated student who owns the certificate (taken from the token, never from the body)</param>
/// <param name="ContentType">The MIME type declared by the client</param>
/// <param name="FileContent">The raw content of the file</param>
/// <param name="HolderName">Holder name read by the OCR, if any</param>
/// <param name="InstitutionName">Issuing institution read by the OCR, if any</param>
/// <param name="CourseName">Course or program name read by the OCR, if any</param>
/// <param name="IssueDate">Issue date read by the OCR, if any</param>
/// <param name="DurationHours">Duration in hours read by the OCR, if any</param>
/// <param name="CertificateNumber">Certificate number read by the OCR, if any</param>
/// <param name="VerificationCode">Verification code read by the OCR, if any</param>
/// <param name="VerificationUrl">Official verification URL read by the OCR, if any</param>
/// <param name="QrPayload">Decoded content of the QR code, if any</param>
/// <param name="OcrText">Full text recognized by the OCR, kept for audit and reprocessing</param>
public record UploadCertificateCommand(
    int OwnerId,
    string ContentType,
    byte[] FileContent,
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