using SkillSwap.Platform.LearningPathEngine.Domain.Model.Entities;

namespace SkillSwap.Platform.LearningPathEngine.Domain.Services;

/// <summary>
///     Contract for generating the questions of an assessment, decoupling the domain from the
///     concrete generative AI provider.
/// </summary>
public interface IQuestionGenerationService
{
    /// <summary>
    ///     Generates exactly <see cref="Model.Aggregates.AssessmentBlueprint.QuestionCount" /> valid
    ///     questions for the given skill.
    /// </summary>
    Task<IReadOnlyList<Question>> GenerateQuestionsAsync(string skillTag, CancellationToken cancellationToken);
}