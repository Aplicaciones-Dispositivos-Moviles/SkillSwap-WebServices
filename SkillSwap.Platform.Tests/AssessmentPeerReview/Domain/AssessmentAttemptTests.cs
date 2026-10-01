using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.Tests.AssessmentPeerReview.Domain;

public class AssessmentAttemptTests
{
    private static readonly int[] Correct = [1, 2, 3, 0, 1];

    private static AssessmentAttempt Attempt(int[] selected, int blueprintId = 1, int studentId = 1)
    {
        return new AssessmentAttempt(blueprintId, studentId, selected, Correct);
    }

    [Fact]
    public void Constructor_WithAllAnswersCorrect_PassesWithFullScore()
    {
        var attempt = Attempt([1, 2, 3, 0, 1]);

        Assert.Equal(5, attempt.Score.Value);
        Assert.Equal(5, attempt.Score.Total);
        Assert.True(attempt.Passed);
        Assert.Equal([1, 2, 3, 0, 1], attempt.SelectedAnswers);
    }

    [Fact]
    public void Constructor_WithFourCorrectAnswers_Passes()
    {
        var attempt = Attempt([1, 2, 3, 0, 3]);

        Assert.Equal(4, attempt.Score.Value);
        Assert.True(attempt.Passed);
    }

    [Fact]
    public void Constructor_WithThreeCorrectAnswers_DoesNotPass()
    {
        var attempt = Attempt([1, 2, 3, 3, 3]);

        Assert.Equal(3, attempt.Score.Value);
        Assert.False(attempt.Passed);
    }

    [Fact]
    public void Constructor_WithNoCorrectAnswers_ScoresZero()
    {
        var attempt = Attempt([0, 0, 0, 1, 0]);

        Assert.Equal(0, attempt.Score.Value);
        Assert.False(attempt.Passed);
    }

    [Fact]
    public void Constructor_SetsTheOwnerBlueprintAndCompletionTime()
    {
        var before = DateTime.UtcNow;

        var attempt = Attempt([1, 2, 3, 0, 1], blueprintId: 9, studentId: 4);

        Assert.Equal(9, attempt.BlueprintId);
        Assert.Equal(4, attempt.StudentId);
        Assert.InRange(attempt.CompletedAt, before, DateTime.UtcNow);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void Constructor_WithAnAnswerOutOfRange_ThrowsDomainException(int answer)
    {
        Assert.Throws<DomainException>(() => Attempt([1, 2, 3, 0, answer]));
    }

    [Fact]
    public void Constructor_WithAWrongNumberOfAnswers_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Attempt([1, 2, 3, 0]));
        Assert.Throws<DomainException>(() => Attempt([1, 2, 3, 0, 1, 1]));
    }

    [Fact]
    public void Constructor_WithTooFewQuestions_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new AssessmentAttempt(1, 1, [1, 2, 3], [1, 2, 3]));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public void Constructor_WithAnInvalidId_ThrowsDomainException(int blueprintId, int studentId)
    {
        Assert.Throws<DomainException>(() => Attempt([1, 2, 3, 0, 1], blueprintId, studentId));
    }

    [Fact]
    public void IncorrectQuestionIndexes_ListsTheWrongAnswers()
    {
        var attempt = Attempt([1, 0, 3, 0, 2]);

        Assert.Equal([1, 4], attempt.IncorrectQuestionIndexes(Correct));
    }

    [Fact]
    public void IncorrectQuestionIndexes_WhenEverythingIsCorrect_IsEmpty()
    {
        Assert.Empty(Attempt([1, 2, 3, 0, 1]).IncorrectQuestionIndexes(Correct));
    }

    [Fact]
    public void IncorrectQuestionIndexes_WithAnotherNumberOfAnswers_ThrowsDomainException()
    {
        var attempt = Attempt([1, 2, 3, 0, 1]);

        Assert.Throws<DomainException>(() => attempt.IncorrectQuestionIndexes([1, 2, 3]));
    }
}