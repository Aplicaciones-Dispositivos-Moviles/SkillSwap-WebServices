using SkillSwap.Platform.Reputation.Domain.Model.Aggregates;
using SkillSwap.Platform.Reputation.Domain.Services;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Tests.Reputation.Domain;

public class StudentEmployabilityScoreTests
{
    private static readonly EmployabilityScoreCalculator Calculator = new();

    [Fact]
    public void Constructor_StartsWithNoSkillsAndAZeroScore()
    {
        var before = DateTime.UtcNow;

        var score = new StudentEmployabilityScore(5);

        Assert.Equal(5, score.StudentId);
        Assert.Equal(0, score.VerifiedSkillsCount);
        Assert.Equal(0, score.Score.Value);
        Assert.InRange(score.UpdatedAt, before, DateTime.UtcNow);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithAnInvalidStudent_ThrowsDomainException(int studentId)
    {
        Assert.Throws<DomainException>(() => new StudentEmployabilityScore(studentId));
    }

    [Fact]
    public void RecordSkillVerified_CountsTheSkillAndRecalculatesTheScore()
    {
        var score = new StudentEmployabilityScore(5).RecordSkillVerified(Calculator).RecordSkillVerified(Calculator);

        Assert.Equal(2, score.VerifiedSkillsCount);
        Assert.Equal(20, score.Score.Value);
    }

    [Fact]
    public void RecordSkillVerified_StopsGrowingAtTheMaximum()
    {
        var score = new StudentEmployabilityScore(5);
        for (var i = 0; i < 12; i++) score.RecordSkillVerified(Calculator);

        Assert.Equal(12, score.VerifiedSkillsCount);
        Assert.Equal(100, score.Score.Value);
    }

    [Fact]
    public void RecordSkillVerified_RefreshesTheUpdatedAt()
    {
        var score = new StudentEmployabilityScore(5);
        var before = DateTime.UtcNow;

        score.RecordSkillVerified(Calculator);

        Assert.InRange(score.UpdatedAt, before, DateTime.UtcNow);
    }

    [Fact]
    public void WithoutACalculator_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new StudentEmployabilityScore(5).RecordSkillVerified(null!));
    }
}