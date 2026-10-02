using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Reputation.Domain.Services;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Tests.Reputation.Domain;

public class VerifierReliabilityTests
{
    private static readonly VerifierReliabilityCalculator Calculator = new();

    [Fact]
    public void Constructor_StartsWithZeroCountersAndTheFullScore()
    {
        var before = DateTime.UtcNow;

        var reliability = new VerifierReliability(3);

        Assert.Equal(3, reliability.VerifierUserId);
        Assert.Equal(0, reliability.ResolvedCasesCount);
        Assert.Equal(0, reliability.OverturnedDecisionsCount);
        Assert.Equal(0, reliability.SanctionsCount);
        Assert.Equal(100, reliability.Score.Value);
        Assert.InRange(reliability.UpdatedAt, before, DateTime.UtcNow);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithAnInvalidUser_ThrowsDomainException(int userId)
    {
        Assert.Throws<DomainException>(() => new VerifierReliability(userId));
    }

    [Fact]
    public void RecordResolution_CountsTheCaseWithoutChangingTheScore()
    {
        var reliability = new VerifierReliability(3).RecordResolution(Calculator).RecordResolution(Calculator);

        Assert.Equal(2, reliability.ResolvedCasesCount);
        Assert.Equal(100, reliability.Score.Value);
    }

    [Fact]
    public void RecordOverturn_CountsItAndDiscountsFifteenPoints()
    {
        var reliability = new VerifierReliability(3).RecordOverturn(Calculator);

        Assert.Equal(1, reliability.OverturnedDecisionsCount);
        Assert.Equal(85, reliability.Score.Value);
    }

    [Fact]
    public void ApplySanction_CountsItAndDiscountsTwentyFivePoints()
    {
        var reliability = new VerifierReliability(3).ApplySanction(Calculator);

        Assert.Equal(1, reliability.SanctionsCount);
        Assert.Equal(75, reliability.Score.Value);
    }

    [Fact]
    public void Events_Combine()
    {
        var reliability = new VerifierReliability(3)
            .RecordResolution(Calculator)
            .RecordResolution(Calculator)
            .RecordResolution(Calculator)
            .RecordOverturn(Calculator)
            .ApplySanction(Calculator);

        Assert.Equal(3, reliability.ResolvedCasesCount);
        Assert.Equal(1, reliability.OverturnedDecisionsCount);
        Assert.Equal(1, reliability.SanctionsCount);
        Assert.Equal(60, reliability.Score.Value);
    }

    [Fact]
    public void Score_NeverDropsBelowZero()
    {
        var reliability = new VerifierReliability(3);
        for (var i = 0; i < 8; i++) reliability.RecordOverturn(Calculator);

        Assert.Equal(8, reliability.OverturnedDecisionsCount);
        Assert.Equal(0, reliability.Score.Value);
    }

    [Fact]
    public void EveryChange_RefreshesTheUpdatedAt()
    {
        var reliability = new VerifierReliability(3);
        var before = DateTime.UtcNow;

        reliability.RecordResolution(Calculator);

        Assert.InRange(reliability.UpdatedAt, before, DateTime.UtcNow);
    }

    [Fact]
    public void WithoutACalculator_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new VerifierReliability(3).RecordResolution(null!));
    }
}