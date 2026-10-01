using SkillSwap.Platform.LearningPathEngine.Domain.Services;
using SkillSwap.Platform.Shared.Domain.Exceptions;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.LearningPathEngine.Domain;

public class SkillGapAnalyzerTests
{
    private readonly SkillGapAnalyzer _analyzer = new(LearningPathTestData.Taxonomy);

    [Fact]
    public void Analyze_WithNothingVerified_RequiresTheGoalAndAllItsPrerequisites()
    {
        var gap = _analyzer.Analyze(LearningPathTestData.Goal("authentication-jwt"), []);

        Assert.Equal(
            ["authentication-jwt", "http-basics", "networking-basics", "programming-fundamentals", "rest-api-design"],
            gap.MissingSkillTags);
        Assert.Empty(gap.VerifiedSkillTags);
    }

    [Fact]
    public void Analyze_WhenAnIntermediateSkillIsVerified_DoesNotRequireItsPrerequisites()
    {
        var gap = _analyzer.Analyze(LearningPathTestData.Goal("authentication-jwt"), ["rest-api-design"]);

        Assert.Equal(["authentication-jwt"], gap.MissingSkillTags);
        Assert.Equal(["rest-api-design"], gap.VerifiedSkillTags);
    }

    [Fact]
    public void Analyze_KeepsRequiringAPrerequisiteReachableThroughAnotherSkill()
    {
        var gap = _analyzer.Analyze(LearningPathTestData.Goal("authentication-jwt", "http-basics"),
            ["rest-api-design"]);

        Assert.Equal(["authentication-jwt", "http-basics", "networking-basics"], gap.MissingSkillTags);
    }

    [Fact]
    public void Analyze_IgnoresVerifiedSkillsUnrelatedToTheGoal()
    {
        var gap = _analyzer.Analyze(LearningPathTestData.Goal("http-basics"), ["sql-fundamentals"]);

        Assert.Equal(["http-basics", "networking-basics"], gap.MissingSkillTags);
        Assert.Empty(gap.VerifiedSkillTags);
    }

    [Fact]
    public void Analyze_WhenTheGoalIsAlreadyVerified_ReturnsAnEmptyGap()
    {
        var gap = _analyzer.Analyze(LearningPathTestData.Goal("authentication-jwt"), ["authentication-jwt"]);

        Assert.True(gap.IsEmpty);
        Assert.Equal(["authentication-jwt"], gap.VerifiedSkillTags);
    }

    [Fact]
    public void Analyze_WithSeveralGoalSkillsSharingPrerequisites_ListsThemOnce()
    {
        var gap = _analyzer.Analyze(LearningPathTestData.Goal("rest-api-design", "http-basics"), []);

        Assert.Equal(["http-basics", "networking-basics", "programming-fundamentals", "rest-api-design"],
            gap.MissingSkillTags);
    }

    [Fact]
    public void Analyze_WithASkillOutsideTheTaxonomy_Throws()
    {
        Assert.Throws<DomainException>(() => _analyzer.Analyze(LearningPathTestData.Goal("unknown-skill"), []));
    }
}