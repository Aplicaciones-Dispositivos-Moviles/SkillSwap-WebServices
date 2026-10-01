using SkillSwap.Platform.CredentialVerification.Domain.Model.Aggregates;
using SkillSwap.Platform.CredentialVerification.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Tests.CredentialVerification.Domain;

public class CertificateTests
{
    private static readonly DateOnly Today = new(2026, 1, 1);

    private static Certificate NewPending()
    {
        return new Certificate(1, "ABCDEF", "certificates/1/file");
    }

    private static Certificate NewWithCompleteData()
    {
        return NewPending().ApplyExtractedData("Ana Perez", "Coursera", "Backend with ASP.NET",
            new DateOnly(2025, 3, 10), 40, "cert-001", "code-xyz", "https://example.com/verify/1", "qr", "full text");
    }

    private static Certificate NewSuspicious()
    {
        return NewWithCompleteData().AssessRisk(new RiskAssessment(60));
    }

    // ---------- Creation ----------

    [Fact]
    public void NewCertificate_StartsPendingWithOcrOnlyMethodAndNoRisk()
    {
        var certificate = NewPending();

        Assert.Equal(VerificationStatus.Pending, certificate.Status);
        Assert.Equal(VerificationMethod.OcrOnly, certificate.VerificationMethod);
        Assert.Null(certificate.RiskAssessment);
        Assert.Null(certificate.VerifiedAt);
        Assert.Equal(DateTimeKind.Utc, certificate.CreatedAt.Kind);
        Assert.Equal("abcdef", certificate.FileHash);
    }

    [Theory]
    [InlineData(0, "hash", "ref")]
    [InlineData(-1, "hash", "ref")]
    [InlineData(1, "", "ref")]
    [InlineData(1, "  ", "ref")]
    [InlineData(1, "hash", "")]
    public void Constructor_WithInvalidValues_ThrowsDomainException(int ownerId, string hash, string reference)
    {
        Assert.Throws<DomainException>(() => new Certificate(ownerId, hash, reference));
    }

    // ---------- Extracted data ----------

    [Fact]
    public void ApplyExtractedData_TrimsValuesAndUppercasesNumberAndCode()
    {
        var certificate = NewPending().ApplyExtractedData("  Ana Perez ", " Coursera", "Course", null, null,
            " cert-001 ", "code-xyz", null, null, null);

        Assert.Equal("Ana Perez", certificate.HolderName);
        Assert.Equal("Coursera", certificate.InstitutionName);
        Assert.Equal("CERT-001", certificate.CertificateNumber);
        Assert.Equal("CODE-XYZ", certificate.VerificationCode);
    }

    [Fact]
    public void ApplyExtractedData_StoresBlankValuesAsNullAndEmptyOcrText()
    {
        var certificate = NewPending().ApplyExtractedData("  ", "", null, null, null, " ", null, "", null, "   ");

        Assert.Null(certificate.HolderName);
        Assert.Null(certificate.InstitutionName);
        Assert.Null(certificate.CertificateNumber);
        Assert.Null(certificate.VerificationUrl);
        Assert.Equal(string.Empty, certificate.OcrText);
    }

    [Fact]
    public void ApplyExtractedData_WithTextOverTheLimit_ThrowsDomainException()
    {
        var tooLong = new string('a', Certificate.MaxTextLength + 1);

        Assert.Throws<DomainException>(() =>
            NewPending().ApplyExtractedData(tooLong, null, null, null, null, null, null, null, null, null));
    }

    [Fact]
    public void ApplyExtractedData_OnANonPendingCertificate_ThrowsDomainException()
    {
        var certificate = NewSuspicious();

        Assert.Throws<DomainException>(() =>
            certificate.ApplyExtractedData(null, null, null, null, null, null, null, null, null, null));
    }

    // ---------- OCR inconsistencies ----------

    [Fact]
    public void HasOcrInconsistencies_WithCompleteAndValidData_ReturnsFalse()
    {
        Assert.False(NewWithCompleteData().HasOcrInconsistencies(Today));
    }

    [Fact]
    public void HasOcrInconsistencies_WhenDurationIsMissing_ReturnsFalse()
    {
        var certificate = NewPending().ApplyExtractedData("Ana", "Coursera", "Course", new DateOnly(2025, 3, 10),
            null, null, null, null, null, null);

        Assert.False(certificate.HasOcrInconsistencies(Today));
    }

    [Theory]
    [InlineData(null, "Coursera", "Course", true)]
    [InlineData("Ana", null, "Course", true)]
    [InlineData("Ana", "Coursera", null, true)]
    [InlineData("Ana", "Coursera", "Course", false)]
    public void HasOcrInconsistencies_WhenHolderInstitutionOrCourseIsMissing_ReturnsTrue(string? holder,
        string? institution, string? course, bool hasIssueDate)
    {
        var certificate = NewPending().ApplyExtractedData(holder, institution, course,
            hasIssueDate ? new DateOnly(2025, 3, 10) : null, null, null, null, null, null, null);

        Assert.Equal(!(holder is not null && institution is not null && course is not null && hasIssueDate),
            certificate.HasOcrInconsistencies(Today));
    }

    [Fact]
    public void HasOcrInconsistencies_WithFutureIssueDate_ReturnsTrue()
    {
        var certificate = NewPending().ApplyExtractedData("Ana", "Coursera", "Course", Today.AddDays(1), 10,
            null, null, null, null, null);

        Assert.True(certificate.HasOcrInconsistencies(Today));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void HasOcrInconsistencies_WithNonPositiveDuration_ReturnsTrue(int hours)
    {
        var certificate = NewPending().ApplyExtractedData("Ana", "Coursera", "Course", new DateOnly(2025, 3, 10),
            hours, null, null, null, null, null);

        Assert.True(certificate.HasOcrInconsistencies(Today));
    }

    // ---------- Risk assessment ----------

    [Theory]
    [InlineData(0, VerificationStatus.Unverified)]
    [InlineData(30, VerificationStatus.Unverified)]
    [InlineData(49, VerificationStatus.Unverified)]
    [InlineData(50, VerificationStatus.Suspicious)]
    [InlineData(85, VerificationStatus.Suspicious)]
    public void AssessRisk_SetsTheStatusFromTheRiskLevel(int score, VerificationStatus expected)
    {
        var certificate = NewWithCompleteData().AssessRisk(new RiskAssessment(score));

        Assert.Equal(expected, certificate.Status);
        Assert.Equal(score, certificate.RiskAssessment!.Score);
        Assert.Null(certificate.VerifiedAt);
    }

    [Fact]
    public void AssessRisk_Twice_ThrowsDomainException()
    {
        var certificate = NewWithCompleteData().AssessRisk(new RiskAssessment(0));

        Assert.Throws<DomainException>(() => certificate.AssessRisk(new RiskAssessment(60)));
    }

    // ---------- Dispute resolution ----------

    [Theory]
    [InlineData(true, VerificationStatus.Verified)]
    [InlineData(false, VerificationStatus.Rejected)]
    public void ResolveDispute_OnASuspiciousCertificate_SetsTheFinalStatus(bool isAuthentic,
        VerificationStatus expected)
    {
        var certificate = NewSuspicious().ResolveDispute(isAuthentic);

        Assert.Equal(expected, certificate.Status);
        Assert.Equal(VerificationMethod.Manual, certificate.VerificationMethod);
        Assert.NotNull(certificate.VerifiedAt);
        Assert.Equal(DateTimeKind.Utc, certificate.VerifiedAt!.Value.Kind);
    }

    [Fact]
    public void ResolveDispute_OnAPendingCertificate_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => NewPending().ResolveDispute(true));
    }

    [Fact]
    public void ResolveDispute_OnAnUnverifiedCertificate_ThrowsDomainException()
    {
        var certificate = NewWithCompleteData().AssessRisk(new RiskAssessment(0));

        Assert.Throws<DomainException>(() => certificate.ResolveDispute(true));
    }

    [Fact]
    public void ResolveDispute_Twice_ThrowsDomainException()
    {
        var certificate = NewSuspicious().ResolveDispute(true);

        Assert.Throws<DomainException>(() => certificate.ResolveDispute(false));
    }
}