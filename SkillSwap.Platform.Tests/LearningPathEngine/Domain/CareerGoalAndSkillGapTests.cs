using SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Tests.LearningPathEngine.Domain;

public class CareerGoalAndSkillGapTests
{
    [Fact]
    public void CareerGoal_TrimsTheTextAndRemovesDuplicateAndBlankTags()
    {
        var goal = new CareerGoal("  build APIs  ", ["rest-api-design", " rest-api-design ", "", "  "]);

        Assert.Equal("build APIs", goal.RawText);
        Assert.Equal(["rest-api-design"], goal.MappedSkillTags);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CareerGoal_WithBlankText_Throws(string text)
    {
        Assert.Throws<DomainException>(() => new CareerGoal(text, ["rest-api-design"]));
    }

    [Fact]
    public void CareerGoal_WithTextOverTheLimit_Throws()
    {
        Assert.Throws<DomainException>(() =>
            new CareerGoal(new string('a', CareerGoal.MaxRawTextLength + 1), ["rest-api-design"]));
    }

    [Fact]
    public void CareerGoal_WithoutAnySkill_Throws()
    {
        Assert.Throws<DomainException>(() => new CareerGoal("build APIs", []));
        Assert.Throws<DomainException>(() => new CareerGoal("build APIs", ["", " "]));
    }

    [Fact]
    public void SkillGap_SortsAndDeduplicatesBothLists()
    {
        var gap = new SkillGap(["b", "a", "a"], ["z", "y", "y"]);

        Assert.Equal(["a", "b"], gap.VerifiedSkillTags);
        Assert.Equal(["y", "z"], gap.MissingSkillTags);
        Assert.False(gap.IsEmpty);
    }

    [Fact]
    public void SkillGap_WithoutMissingSkills_IsEmpty()
    {
        Assert.True(new SkillGap(["a"], []).IsEmpty);
    }

    [Fact]
    public void SkillGap_WithASkillBothVerifiedAndMissing_Throws()
    {
        Assert.Throws<DomainException>(() => new SkillGap(["a"], ["a", "b"]));
    }
}