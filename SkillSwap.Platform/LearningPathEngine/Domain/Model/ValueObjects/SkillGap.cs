using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;

/// <summary>
///     The difference between what the student has already demonstrated and what the goal requires.
///     Both lists are distinct and sorted, so the same input always produces the same gap.
/// </summary>
public sealed record SkillGap
{
    public SkillGap(IEnumerable<string> verifiedSkillTags, IEnumerable<string> missingSkillTags)
    {
        var verified = Normalize(verifiedSkillTags);
        var missing = Normalize(missingSkillTags);
        if (verified.Intersect(missing, StringComparer.Ordinal).Any())
            throw new DomainException("A skill cannot be both verified and missing.");

        VerifiedSkillTags = verified;
        MissingSkillTags = missing;
    }

    /// <summary>
    ///     Skills relevant to the goal that the student already demonstrated.
    /// </summary>
    public IReadOnlyList<string> VerifiedSkillTags { get; }

    /// <summary>
    ///     Skills still to be demonstrated, including the prerequisites of the goal skills.
    /// </summary>
    public IReadOnlyList<string> MissingSkillTags { get; }

    public bool IsEmpty => MissingSkillTags.Count == 0;

    private static List<string> Normalize(IEnumerable<string> tags)
    {
        return tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(tag => tag, StringComparer.Ordinal)
            .ToList();
    }
}