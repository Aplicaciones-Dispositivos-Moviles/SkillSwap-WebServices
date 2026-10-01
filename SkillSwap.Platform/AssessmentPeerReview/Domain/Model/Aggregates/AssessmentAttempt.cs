using SkillSwap.Platform.AssessmentPeerReview.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.AssessmentPeerReview.Domain.Model.Aggregates;

/// <summary>
///     AssessmentAttempt aggregate root
/// </summary>
/// <remarks>
///     A student's attempt at the assessment of a node. The score is always computed on the server,
///     against the correct answers of the blueprint, so the client cannot tamper with it.
/// </remarks>
public class AssessmentAttempt
{
    public const int AnswerOptionCount = 4;
    public const int PassingScore = 4;

    /// <summary>
    ///     Parameterless constructor required by EF Core.
    /// </summary>
    protected AssessmentAttempt()
    {
        Score = null!;
        SelectedAnswers = [];
    }

    public AssessmentAttempt(int blueprintId, int studentId, IReadOnlyList<int> selectedAnswers,
        IReadOnlyList<int> correctAnswers)
    {
        if (blueprintId <= 0)
            throw new DomainException("The attempt must belong to a valid blueprint.");
        if (studentId <= 0)
            throw new DomainException("The attempt must belong to a valid student.");
        if (correctAnswers is null || correctAnswers.Count < PassingScore)
            throw new DomainException($"An assessment needs at least {PassingScore} questions.");
        if (selectedAnswers is null || selectedAnswers.Count != correctAnswers.Count)
            throw new DomainException("There must be exactly one answer per question.");
        if (selectedAnswers.Any(answer => answer < 0 || answer >= AnswerOptionCount))
            throw new DomainException($"Each answer must be an index between 0 and {AnswerOptionCount - 1}.");

        var hits = selectedAnswers.Where((answer, index) => answer == correctAnswers[index]).Count();

        BlueprintId = blueprintId;
        StudentId = studentId;
        SelectedAnswers = selectedAnswers.ToList();
        Score = new Score(hits, correctAnswers.Count);
        Passed = hits >= PassingScore;
        CompletedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public int BlueprintId { get; private set; }
    public int StudentId { get; private set; }
    public IReadOnlyList<int> SelectedAnswers { get; private set; }
    public Score Score { get; private set; }
    public bool Passed { get; private set; }
    public DateTime CompletedAt { get; private set; }

    /// <summary>
    ///     The positions of the questions that were answered incorrectly.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the number of correct answers does not match the attempt.</exception>
    public IReadOnlyList<int> IncorrectQuestionIndexes(IReadOnlyList<int> correctAnswers)
    {
        if (correctAnswers is null || correctAnswers.Count != SelectedAnswers.Count)
            throw new DomainException("The correct answers do not match the attempt.");

        return SelectedAnswers
            .Select((answer, index) => (answer, index))
            .Where(pair => pair.answer != correctAnswers[pair.index])
            .Select(pair => pair.index)
            .ToList();
    }
}