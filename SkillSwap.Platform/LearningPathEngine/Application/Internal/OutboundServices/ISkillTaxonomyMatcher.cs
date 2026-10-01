namespace SkillSwap.Platform.LearningPathEngine.Application.Internal.OutboundServices;

/// <summary>
///     Contract for interpreting free text against the skill taxonomy, decoupling the application from
///     the matching technique (keywords today, possibly semantic search later).
/// </summary>
public interface ISkillTaxonomyMatcher
{
    /// <summary>
    ///     The tags of the skills the text refers to. Empty when it matches none.
    /// </summary>
    IReadOnlyList<string> Match(string text);
}