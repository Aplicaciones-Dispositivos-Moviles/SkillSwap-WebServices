using SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.LearningPathEngine.Domain.Services;

/// <summary>
///     Contract for computing the gap between what a student has demonstrated and what a goal requires.
/// </summary>
public interface ISkillGapAnalyzer
{
    SkillGap Analyze(CareerGoal goal, IReadOnlyCollection<string> verifiedSkillTags);
}