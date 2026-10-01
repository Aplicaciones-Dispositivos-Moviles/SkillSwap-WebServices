using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;

/// <summary>
///     The goal declared by the student in free text, together with its translation into skills
///     of the internal taxonomy.
/// </summary>
public sealed record CareerGoal
{
    public const int MaxRawTextLength = 500;

    public CareerGoal(string rawText, IEnumerable<string> mappedSkillTags)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            throw new DomainException("The goal text cannot be empty.");

        var text = rawText.Trim();
        if (text.Length > MaxRawTextLength)
            throw new DomainException($"The goal text cannot exceed {MaxRawTextLength} characters.");

        var tags = mappedSkillTags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (tags.Count == 0)
            throw new DomainException("The goal could not be interpreted: no skill of the taxonomy matches it.");

        RawText = text;
        MappedSkillTags = tags;
    }

    public string RawText { get; }
    public IReadOnlyList<string> MappedSkillTags { get; }
}