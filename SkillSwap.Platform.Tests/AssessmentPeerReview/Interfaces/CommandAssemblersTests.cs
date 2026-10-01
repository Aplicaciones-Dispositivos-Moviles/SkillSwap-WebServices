using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Resources;
using SkillSwap.Platform.AssessmentPeerReview.Interfaces.Rest.Transform;

namespace SkillSwap.Platform.Tests.AssessmentPeerReview.Interfaces;

public class CommandAssemblersTests
{
    // ---------- Resolve ----------

    [Theory]
    [InlineData("Approved", ReviewDecision.Approved)]
    [InlineData("approved", ReviewDecision.Approved)]
    [InlineData("Rejected", ReviewDecision.Rejected)]
    [InlineData("REJECTED", ReviewDecision.Rejected)]
    public void Resolve_ParsesTheDecisionIgnoringCase(string decision, ReviewDecision expected)
    {
        var command = ResolveVerificationCaseCommandFromResourceAssembler.ToCommandFromResource(
            4, new ResolveCaseResource(decision, "Meets the rubric."), 9);

        Assert.Equal(expected, command.Decision);
        Assert.Equal(4, command.CaseId);
        Assert.Equal(9, command.VerifierUserId);
        Assert.Equal("Meets the rubric.", command.RubricNotes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("maybe")]
    [InlineData("99")]
    public void Resolve_WithAnInvalidDecision_ProducesAnUndefinedValue(string? decision)
    {
        var command = ResolveVerificationCaseCommandFromResourceAssembler.ToCommandFromResource(
            4, new ResolveCaseResource(decision, "Notes"), 9);

        Assert.False(Enum.IsDefined(command.Decision));
    }

    [Fact]
    public void Resolve_WithoutNotes_UsesAnEmptyString()
    {
        var command = ResolveVerificationCaseCommandFromResourceAssembler.ToCommandFromResource(
            4, new ResolveCaseResource("Approved", null), 9);

        Assert.Equal(string.Empty, command.RubricNotes);
    }

    // ---------- Submit ----------

    [Fact]
    public void Submit_TakesTheStudentFromTheCallerAndKeepsTheAnswers()
    {
        var command = SubmitAssessmentAttemptCommandFromResourceAssembler.ToCommandFromResource(
            new SubmitAssessmentAttemptResource(3, [1, 2, 3, 0, 1]), 7);

        Assert.Equal(7, command.StudentId);
        Assert.Equal(3, command.BlueprintId);
        Assert.Equal([1, 2, 3, 0, 1], command.SelectedAnswers);
    }

    [Fact]
    public void Submit_WithoutAnswers_UsesAnEmptyList()
    {
        var command = SubmitAssessmentAttemptCommandFromResourceAssembler.ToCommandFromResource(
            new SubmitAssessmentAttemptResource(3, null), 7);

        Assert.Empty(command.SelectedAnswers);
    }

    // ---------- Evidence, profile and availability ----------

    [Fact]
    public void Attach_TakesTheStudentFromTheCallerAndDefaultsTheLinkToEmpty()
    {
        var command = AttachCaseEvidenceCommandFromResourceAssembler.ToCommandFromResource(
            4, new AttachEvidenceResource(null), 7);

        Assert.Equal(4, command.CaseId);
        Assert.Equal(7, command.StudentId);
        Assert.Equal(string.Empty, command.EvidenceUrl);
    }

    [Fact]
    public void CreateProfile_TakesTheUserFromTheCallerAndDefaultsTheSkillToEmpty()
    {
        var command = CreateVerifierProfileCommandFromResourceAssembler.ToCommandFromResource(
            new CreateVerifierProfileResource(null), 7);

        Assert.Equal(7, command.UserId);
        Assert.Equal(string.Empty, command.SkillTag);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Availability_KeepsTheValueSent(bool available)
    {
        var command = UpdateVerifierAvailabilityCommandFromResourceAssembler.ToCommandFromResource(
            new VerifierAvailabilityResource(available), 7);

        Assert.Equal(7, command.UserId);
        Assert.Equal(available, command.Available);
    }
}