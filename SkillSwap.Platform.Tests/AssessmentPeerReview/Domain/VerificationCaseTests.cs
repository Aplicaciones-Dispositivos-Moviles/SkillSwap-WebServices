using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Tests.AssessmentPeerReview.Domain;

public class VerificationCaseTests
{
    private const int StudentId = 1;
    private const int VerifierId = 2;

    private static VerificationCase NewCase()
    {
        return new VerificationCase(10, StudentId, 5, " http-basics ");
    }

    private static VerificationCase AssignedCase()
    {
        return NewCase().AssignVerifier(VerifierId);
    }

    // ---------- Constructor ----------

    [Fact]
    public void Constructor_OpensAPendingCaseWithoutVerifier()
    {
        var before = DateTime.UtcNow;

        var verificationCase = NewCase();

        Assert.Equal(CaseStatus.Pending, verificationCase.Status);
        Assert.Equal(10, verificationCase.AttemptId);
        Assert.Equal(StudentId, verificationCase.StudentId);
        Assert.Equal(5, verificationCase.PathNodeId);
        Assert.Equal("http-basics", verificationCase.SkillTag);
        Assert.Null(verificationCase.VerifierUserId);
        Assert.Null(verificationCase.Decision);
        Assert.True(verificationCase.IsOpen);
        Assert.InRange(verificationCase.OpenedAt, before, DateTime.UtcNow);
    }

    [Theory]
    [InlineData(0, 1, 1, "skill")]
    [InlineData(1, 0, 1, "skill")]
    [InlineData(1, 1, 0, "skill")]
    [InlineData(1, 1, 1, " ")]
    public void Constructor_WithInvalidData_ThrowsDomainException(int attemptId, int studentId, int pathNodeId,
        string skillTag)
    {
        Assert.Throws<DomainException>(() => new VerificationCase(attemptId, studentId, pathNodeId, skillTag));
    }

    // ---------- Assign ----------

    [Fact]
    public void AssignVerifier_MovesTheCaseToAssigned()
    {
        var verificationCase = AssignedCase();

        Assert.Equal(CaseStatus.Assigned, verificationCase.Status);
        Assert.Equal(VerifierId, verificationCase.VerifierUserId);
        Assert.NotNull(verificationCase.AssignedAt);
        Assert.True(verificationCase.IsAssignedTo(VerifierId));
        Assert.False(verificationCase.IsAssignedTo(99));
    }

    [Fact]
    public void AssignVerifier_ToTheStudentOfTheCase_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => NewCase().AssignVerifier(StudentId));
    }

    [Fact]
    public void AssignVerifier_WithAnInvalidVerifier_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => NewCase().AssignVerifier(0));
    }

    [Fact]
    public void AssignVerifier_ToACaseAlreadyAssigned_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => AssignedCase().AssignVerifier(3));
    }

    // ---------- Evidence ----------

    [Theory]
    [InlineData("https://github.com/student/project")]
    [InlineData("http://portfolio.example.com/work")]
    public void AttachEvidence_WithAValidLink_KeepsItTrimmed(string url)
    {
        var verificationCase = NewCase().AttachEvidence($"  {url}  ");

        Assert.Equal(url, verificationCase.EvidenceUrl);
    }

    [Fact]
    public void AttachEvidence_ToAnAssignedCase_IsAllowedAndReplacesThePreviousLink()
    {
        var verificationCase = AssignedCase()
            .AttachEvidence("https://github.com/student/one")
            .AttachEvidence("https://github.com/student/two");

        Assert.Equal("https://github.com/student/two", verificationCase.EvidenceUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("ftp://files.example.com/work")]
    [InlineData("javascript:alert(1)")]
    public void AttachEvidence_WithAnInvalidLink_ThrowsDomainException(string url)
    {
        Assert.Throws<DomainException>(() => NewCase().AttachEvidence(url));
    }

    [Fact]
    public void AttachEvidence_WithATooLongLink_ThrowsDomainException()
    {
        var url = "https://example.com/" + new string('a', VerificationCase.MaxEvidenceUrlLength);

        Assert.Throws<DomainException>(() => NewCase().AttachEvidence(url));
    }

    [Fact]
    public void AttachEvidence_ToAResolvedCase_ThrowsDomainException()
    {
        var verificationCase = AssignedCase().Resolve(ReviewDecision.Rejected, "Needs work.");

        Assert.Throws<DomainException>(() => verificationCase.AttachEvidence("https://github.com/student/project"));
    }

    // ---------- Resolve ----------

    [Theory]
    [InlineData(ReviewDecision.Approved)]
    [InlineData(ReviewDecision.Rejected)]
    public void Resolve_RecordsTheDecisionAndTheNotes(ReviewDecision decision)
    {
        var before = DateTime.UtcNow;

        var verificationCase = AssignedCase().Resolve(decision, "  Good use of status codes.  ");

        Assert.Equal(CaseStatus.Resolved, verificationCase.Status);
        Assert.Equal(decision, verificationCase.Decision);
        Assert.Equal("Good use of status codes.", verificationCase.RubricNotes);
        Assert.InRange(verificationCase.ResolvedAt!.Value, before, DateTime.UtcNow);
        Assert.False(verificationCase.IsOpen);
    }

    [Fact]
    public void Resolve_APendingCase_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => NewCase().Resolve(ReviewDecision.Approved, "Notes"));
    }

    [Fact]
    public void Resolve_AResolvedCase_ThrowsDomainException()
    {
        var verificationCase = AssignedCase().Resolve(ReviewDecision.Approved, "Notes");

        Assert.Throws<DomainException>(() => verificationCase.Resolve(ReviewDecision.Rejected, "Other"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_WithoutNotes_ThrowsDomainException(string notes)
    {
        Assert.Throws<DomainException>(() => AssignedCase().Resolve(ReviewDecision.Approved, notes));
    }

    [Fact]
    public void Resolve_WithTooLongNotes_ThrowsDomainException()
    {
        var notes = new string('a', VerificationCase.MaxRubricNotesLength + 1);

        Assert.Throws<DomainException>(() => AssignedCase().Resolve(ReviewDecision.Approved, notes));
    }

    [Fact]
    public void Resolve_WithAnUndefinedDecision_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => AssignedCase().Resolve((ReviewDecision)99, "Notes"));
    }
}