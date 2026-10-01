using SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;

namespace SkillSwap.Platform.LearningPathEngine.Domain.Services;

/// <summary>
///     Walks the prerequisites of the goal skills. A skill the student already demonstrated is not
///     required again, and neither are the prerequisites reachable only through it.
/// </summary>
/// <param name="taxonomy">The skill taxonomy</param>
public class SkillGapAnalyzer(ISkillTaxonomy taxonomy) : ISkillGapAnalyzer
{
    /// <inheritdoc />
    public SkillGap Analyze(CareerGoal goal, IReadOnlyCollection<string> verifiedSkillTags)
    {
        var verified = new HashSet<string>(verifiedSkillTags, StringComparer.Ordinal);
        var missing = new HashSet<string>(StringComparer.Ordinal);
        var relevantVerified = new HashSet<string>(StringComparer.Ordinal);

        var pending = new Stack<string>(goal.MappedSkillTags);
        while (pending.Count > 0)
        {
            var tag = pending.Pop();
            if (verified.Contains(tag))
            {
                relevantVerified.Add(tag);
                continue;
            }

            if (!missing.Add(tag)) continue;
            foreach (var prerequisite in taxonomy.PrerequisitesOf(tag)) pending.Push(prerequisite);
        }

        return new SkillGap(relevantVerified, missing);
    }
}