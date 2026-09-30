using SkillSwap.Platform.CredentialVerification.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.CredentialVerification.Domain.Model.Aggregates;

/// <summary>
///     Certificate aggregate root
/// </summary>
/// <remarks>
///     Centralizes the document uploaded by a student, the data extracted from it by OCR on the
///     mobile device, and the verification state resulting from the risk evaluation. It knows
///     nothing about issuer-specific logic (SUNEDU, Coursera, etc.).
/// </remarks>
public class Certificate
{
    public const int MaxTextLength = 255;
    public const int MaxUrlLength = 2048;
    public const int MaxQrPayloadLength = 4096;
    public const int MaxOcrTextLength = 50000;

    /// <summary>
    ///     Parameterless constructor required by EF Core.
    /// </summary>
    protected Certificate()
    {
        FileHash = null!;
        StorageReference = null!;
        OcrText = string.Empty;
    }

    /// <summary>
    ///     Registers an uploaded document in the <see cref="VerificationStatus.Pending" /> state,
    ///     before the extracted data and the risk assessment are applied.
    /// </summary>
    public Certificate(int ownerId, string fileHash, string storageReference)
    {
        if (ownerId <= 0)
            throw new DomainException("The certificate must belong to a valid user.");
        if (string.IsNullOrWhiteSpace(fileHash))
            throw new DomainException("The file hash cannot be empty.");
        if (string.IsNullOrWhiteSpace(storageReference))
            throw new DomainException("The storage reference cannot be empty.");

        OwnerId = ownerId;
        FileHash = fileHash.Trim().ToLowerInvariant();
        StorageReference = storageReference.Trim();
        OcrText = string.Empty;
        Status = VerificationStatus.Pending;
        VerificationMethod = VerificationMethod.OcrOnly;
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public int OwnerId { get; private set; }
    public string? HolderName { get; private set; }
    public string? InstitutionName { get; private set; }
    public string? CourseName { get; private set; }
    public DateOnly? IssueDate { get; private set; }
    public int? DurationHours { get; private set; }
    public string? CertificateNumber { get; private set; }
    public string? VerificationCode { get; private set; }
    public string? VerificationUrl { get; private set; }
    public string? QrPayload { get; private set; }
    public string OcrText { get; private set; }
    public string FileHash { get; private set; }
    public string StorageReference { get; private set; }
    public VerificationStatus Status { get; private set; }
    public VerificationMethod VerificationMethod { get; private set; }
    public RiskAssessment? RiskAssessment { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? VerifiedAt { get; private set; }

    /// <summary>
    ///     Completes the aggregate with the data read from the document. Every field is optional:
    ///     the OCR may not have been able to read some of them. Blank values are stored as null;
    ///     the certificate number and verification code are stored in uppercase so duplicates
    ///     are detected regardless of case.
    /// </summary>
    /// <exception cref="DomainException">
    ///     Thrown when the certificate is not pending or a value exceeds its maximum length.
    /// </exception>
    public Certificate ApplyExtractedData(
        string? holderName,
        string? institutionName,
        string? courseName,
        DateOnly? issueDate,
        int? durationHours,
        string? certificateNumber,
        string? verificationCode,
        string? verificationUrl,
        string? qrPayload,
        string? ocrText)
    {
        EnsureStatus(VerificationStatus.Pending, "Extracted data can only be applied to a pending certificate.");

        HolderName = Clean(holderName, MaxTextLength, nameof(holderName));
        InstitutionName = Clean(institutionName, MaxTextLength, nameof(institutionName));
        CourseName = Clean(courseName, MaxTextLength, nameof(courseName));
        IssueDate = issueDate;
        DurationHours = durationHours;
        CertificateNumber = Clean(certificateNumber, MaxTextLength, nameof(certificateNumber))?.ToUpperInvariant();
        VerificationCode = Clean(verificationCode, MaxTextLength, nameof(verificationCode))?.ToUpperInvariant();
        VerificationUrl = Clean(verificationUrl, MaxUrlLength, nameof(verificationUrl));
        QrPayload = Clean(qrPayload, MaxQrPayloadLength, nameof(qrPayload));
        OcrText = Clean(ocrText, MaxOcrTextLength, nameof(ocrText)) ?? string.Empty;
        return this;
    }

    /// <summary>
    ///     Whether the extracted data shows OCR inconsistencies: the holder, institution, course or
    ///     issue date could not be read, the issue date is in the future, or the duration is not positive.
    /// </summary>
    /// <param name="today">The current date, received as a parameter to keep the domain deterministic.</param>
    public bool HasOcrInconsistencies(DateOnly today)
    {
        return HolderName is null
               || InstitutionName is null
               || CourseName is null
               || IssueDate is null
               || IssueDate > today
               || DurationHours is <= 0;
    }

    /// <summary>
    ///     Assigns the risk evaluation and moves the certificate to <see cref="VerificationStatus.Suspicious" />
    ///     when the level is high risk, or to <see cref="VerificationStatus.Unverified" /> otherwise
    ///     (low risk or review).
    /// </summary>
    /// <exception cref="DomainException">Thrown when the certificate is not pending.</exception>
    public Certificate AssessRisk(RiskAssessment riskAssessment)
    {
        EnsureStatus(VerificationStatus.Pending, "Risk can only be assessed on a pending certificate.");

        RiskAssessment = riskAssessment;
        Status = riskAssessment.Level == RiskLevel.HighRisk
            ? VerificationStatus.Suspicious
            : VerificationStatus.Unverified;
        return this;
    }

    /// <summary>
    ///     Applies the Coordinator's decision on an escalated certificate, moving it to
    ///     <see cref="VerificationStatus.Verified" /> or <see cref="VerificationStatus.Rejected" />.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the certificate is not suspicious.</exception>
    public Certificate ResolveDispute(bool isAuthentic)
    {
        EnsureStatus(VerificationStatus.Suspicious, "Only a suspicious certificate can be resolved.");

        Status = isAuthentic ? VerificationStatus.Verified : VerificationStatus.Rejected;
        VerificationMethod = VerificationMethod.Manual;
        VerifiedAt = DateTime.UtcNow;
        return this;
    }

    private void EnsureStatus(VerificationStatus expected, string message)
    {
        if (Status != expected) throw new DomainException(message);
    }

    private static string? Clean(string? value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} cannot exceed {maxLength} characters.");
        return trimmed;
    }
}