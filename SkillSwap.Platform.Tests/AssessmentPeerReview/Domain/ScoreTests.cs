using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Tests.AssessmentPeerReview.Domain;

public class ScoreTests
{
    [Theory]
    [InlineData(0, 5)]
    [InlineData(3, 5)]
    [InlineData(5, 5)]
    public void Constructor_WithAValidScore_KeepsIt(int value, int total)
    {
        var score = new Score(value, total);

        Assert.Equal(value, score.Value);
        Assert.Equal(total, score.Total);
    }

    [Theory]
    [InlineData(-1, 5)]
    [InlineData(6, 5)]
    [InlineData(0, 0)]
    [InlineData(0, -1)]
    public void Constructor_WithAnInvalidScore_ThrowsDomainException(int value, int total)
    {
        Assert.Throws<DomainException>(() => new Score(value, total));
    }
}