using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Tests.AssessmentPeerReview.Domain;

public class VerifierProfileTests
{
    [Fact]
    public void Constructor_CreatesAnAvailableAndEnabledProfileWithTheSkill()
    {
        var profile = new VerifierProfile(3, " http-basics ");

        Assert.Equal(3, profile.VerifierUserId);
        Assert.Equal(["http-basics"], profile.SkillTags);
        Assert.True(profile.Available);
        Assert.True(profile.Verified);
        Assert.Equal(0, profile.Rating);
        Assert.Equal(0, profile.ReviewCount);
    }

    [Theory]
    [InlineData(0, "skill")]
    [InlineData(1, " ")]
    public void Constructor_WithInvalidData_ThrowsDomainException(int userId, string skillTag)
    {
        Assert.Throws<DomainException>(() => new VerifierProfile(userId, skillTag));
    }

    [Fact]
    public void AddSkill_WithANewSkill_AddsItKeepingTheListSorted()
    {
        var profile = new VerifierProfile(3, "rest-api-design");

        var added = profile.AddSkill("http-basics");

        Assert.True(added);
        Assert.Equal(["http-basics", "rest-api-design"], profile.SkillTags);
    }

    [Fact]
    public void AddSkill_WithASkillAlreadyEnabled_ChangesNothing()
    {
        var profile = new VerifierProfile(3, "http-basics");

        var added = profile.AddSkill("http-basics");

        Assert.False(added);
        Assert.Single(profile.SkillTags);
    }

    [Fact]
    public void AddSkill_WithABlankSkill_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new VerifierProfile(3, "http-basics").AddSkill(" "));
    }

    [Fact]
    public void CanReview_IsTrueOnlyForEnabledSkills()
    {
        var profile = new VerifierProfile(3, "http-basics");

        Assert.True(profile.CanReview("http-basics"));
        Assert.False(profile.CanReview("sql-fundamentals"));
    }

    [Fact]
    public void CanReview_AfterRevoking_IsFalse()
    {
        var profile = new VerifierProfile(3, "http-basics").Revoke();

        Assert.False(profile.Verified);
        Assert.False(profile.Available);
        Assert.False(profile.CanReview("http-basics"));
    }

    [Fact]
    public void SetAvailability_SwitchesTheAvailability()
    {
        var profile = new VerifierProfile(3, "http-basics");

        Assert.False(profile.SetAvailability(false).Available);
        Assert.True(profile.SetAvailability(true).Available);
    }

    [Fact]
    public void IncrementReviewCount_AddsOneEachTime()
    {
        var profile = new VerifierProfile(3, "http-basics").IncrementReviewCount().IncrementReviewCount();

        Assert.Equal(2, profile.ReviewCount);
    }

    [Fact]
    public void UpdateRating_KeepsTheValue()
    {
        Assert.Equal(87.5, new VerifierProfile(3, "http-basics").UpdateRating(87.5).Rating);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void UpdateRating_WithAnInvalidValue_ThrowsDomainException(double rating)
    {
        Assert.Throws<DomainException>(() => new VerifierProfile(3, "http-basics").UpdateRating(rating));
    }
}