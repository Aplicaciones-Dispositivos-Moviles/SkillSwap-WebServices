using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.LearningPathEngine.Domain.Model.Entities;

/// <summary>
///     A multiple-choice question of an assessment: four distinct answers, one of them correct.
///     The correct answer never leaves the server toward the client.
/// </summary>
public sealed class Question
{
    public const int AnswerCount = 4;
    public const int MaxQuestionLength = 1000;
    public const int MaxAnswerLength = 500;

    public Question(string questionString, IReadOnlyList<string> answers, int correctAnswer)
    {
        if (string.IsNullOrWhiteSpace(questionString))
            throw new DomainException("The question text cannot be empty.");
        var text = questionString.Trim();
        if (text.Length > MaxQuestionLength)
            throw new DomainException($"The question text cannot exceed {MaxQuestionLength} characters.");

        if (answers is null || answers.Count != AnswerCount)
            throw new DomainException($"A question needs exactly {AnswerCount} answers.");
        var cleaned = answers.Select(answer => answer?.Trim() ?? string.Empty).ToList();
        if (cleaned.Any(string.IsNullOrEmpty))
            throw new DomainException("An answer cannot be empty.");
        if (cleaned.Any(answer => answer.Length > MaxAnswerLength))
            throw new DomainException($"An answer cannot exceed {MaxAnswerLength} characters.");
        if (cleaned.Distinct(StringComparer.OrdinalIgnoreCase).Count() != AnswerCount)
            throw new DomainException("The answers of a question must be different from each other.");

        if (correctAnswer is < 0 or >= AnswerCount)
            throw new DomainException($"The correct answer must be an index between 0 and {AnswerCount - 1}.");

        QuestionString = text;
        Answers = cleaned;
        CorrectAnswer = correctAnswer;
    }

    public string QuestionString { get; }
    public IReadOnlyList<string> Answers { get; }
    public int CorrectAnswer { get; }
}