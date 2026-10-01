using SkillSwap.Platform.LearningPathEngine.Application.Internal.OutboundServices;

namespace SkillSwap.Platform.LearningPathEngine.Infrastructure.Taxonomy;

/// <summary>
///     Interprets free text by keyword matching against the skill catalog. A keyword matches only as a
///     whole word or phrase ("java" does not match "javascript"), ignoring case and accents. No embeddings
///     or external service are involved.
/// </summary>
public class SkillTaxonomyMatcher : ISkillTaxonomyMatcher
{
    private readonly IReadOnlyList<(string Tag, string[] PaddedKeywords)> _entries;

    public SkillTaxonomyMatcher(SkillCatalog catalog)
    {
        _entries = catalog.Skills
            .Select(skill => (
                skill.Tag,
                skill.Keywords
                    .Select(keyword => $" {TextNormalizer.Normalize(keyword)} ")
                    .Where(padded => padded.Length > 2)
                    .Distinct()
                    .ToArray()))
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Match(string text)
    {
        var normalized = TextNormalizer.Normalize(text);
        if (normalized.Length == 0) return [];

        var padded = $" {normalized} ";
        return _entries
            .Where(entry => entry.PaddedKeywords.Any(keyword => padded.Contains(keyword, StringComparison.Ordinal)))
            .Select(entry => entry.Tag)
            .ToList();
    }
}