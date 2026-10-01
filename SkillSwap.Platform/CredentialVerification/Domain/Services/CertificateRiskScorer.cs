using SkillSwap.Platform.CredentialVerification.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.CredentialVerification.Domain.Services;

/// <summary>
///     Domain service implementation: pure business rules with no external dependencies.
/// </summary>
public class CertificateRiskScorer : ICertificateRiskScorer
{
    public const int DuplicateCertificateNumberPoints = 30;
    public const int DuplicateVerificationCodePoints = 30;
    public const int OcrInconsistenciesPoints = 15;
    /// <summary>
    ///     A file already registered by another student is enough, on its own, to be high risk.
    /// </summary>
    public const int DuplicateFileHashPoints = RiskAssessment.HighRiskThreshold;

    /// <inheritdoc />
    public RiskAssessment CalculateRisk(
        bool duplicateCertificateNumber,
        bool duplicateVerificationCode,
        bool duplicateFileHash,
        bool ocrInconsistencies)
    {
        var score = 0;
        if (duplicateCertificateNumber) score += DuplicateCertificateNumberPoints;
        if (duplicateVerificationCode) score += DuplicateVerificationCodePoints;
        if (ocrInconsistencies) score += OcrInconsistenciesPoints;
        if (duplicateFileHash) score += DuplicateFileHashPoints;
        return new RiskAssessment(score);
    }
}