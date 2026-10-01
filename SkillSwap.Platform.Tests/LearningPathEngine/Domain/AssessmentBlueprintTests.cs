using SkillSwap.Platform.LearningPathEngine.Domain.Model.Aggregates;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.Entities;
using SkillSwap.Platform.Shared.Domain.Exceptions;
using SkillSwap.Platform.Tests.Support;

namespace SkillSwap.Platform.Tests.LearningPathEngine.Domain;

public class AssessmentBlueprintTests
{
    // ---------- Question ----------

    [Fact]
    public void Question_TrimsTheTextAndTheAnswers()
    {
        var question = new Question("  What is 200?  ", [" OK ", "Created", "Not Found", "Teapot"], 0);

        Assert.Equal("What is 200?", question.QuestionString);
        Assert.Equal("OK", question.Answers[0]);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void Question_WithADifferentNumberOfAnswers_Throws(int count)
    {
        var answers = Enumerable.Range(1, count).Select(i => $"answer {i}").ToList();

        Assert.Throws<DomainException>(() => new Question("Question?", answers, 0));
    }

    [Fact]
    public void Question_WithBlankOrRepeatedAnswers_Throws()
    {
        Assert.Throws<DomainException>(() => new Question("Question?", ["a", "b", "c", " "], 0));
        Assert.Throws<DomainException>(() => new Question("Question?", ["a", "b", "c", "A"], 0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void Question_WithACorrectAnswerOutOfRange_Throws(int index)
    {
        Assert.Throws<DomainException>(() => new Question("Question?", ["a", "b", "c", "d"], index));
    }

    [Fact]
    public void Question_WithBlankText_Throws()
    {
        Assert.Throws<DomainException>(() => new Question("  ", ["a", "b", "c", "d"], 0));
    }

    // ---------- AssessmentBlueprint ----------

    [Fact]
    public void Blueprint_WithTheRightNumberOfQuestions_IsCreated()
    {
        var blueprint = new AssessmentBlueprint(7, " rest-api-design ", LearningPathTestData.Questions(5));

        Assert.Equal(7, blueprint.PathNodeId);
        Assert.Equal("rest-api-design", blueprint.SkillTag);
        Assert.Equal(AssessmentBlueprint.QuestionCount, blueprint.Questions.Count);
        Assert.Equal(DateTimeKind.Utc, blueprint.GeneratedAt.Kind);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    public void Blueprint_WithAWrongNumberOfQuestions_Throws(int count)
    {
        Assert.Throws<DomainException>(() =>
            new AssessmentBlueprint(7, "rest-api-design", LearningPathTestData.Questions(count)));
    }

    [Fact]
    public void Blueprint_WithRepeatedQuestions_Throws()
    {
        var questions = LearningPathTestData.Questions(4);
        questions.Add(new Question("QUESTION 1?", ["w", "x", "y", "z"], 0));

        Assert.Throws<DomainException>(() => new AssessmentBlueprint(7, "rest-api-design", questions));
    }

    [Fact]
    public void Blueprint_WithInvalidNodeOrSkill_Throws()
    {
        Assert.Throws<DomainException>(() =>
            new AssessmentBlueprint(0, "rest-api-design", LearningPathTestData.Questions(5)));
        Assert.Throws<DomainException>(() => new AssessmentBlueprint(7, " ", LearningPathTestData.Questions(5)));
    }
}