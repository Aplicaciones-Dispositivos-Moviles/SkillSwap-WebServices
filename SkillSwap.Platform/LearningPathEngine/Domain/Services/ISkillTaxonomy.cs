namespace SkillSwap.Platform.LearningPathEngine.Domain.Services;

/// <summary>
///     Read-only access to the internal skill taxonomy: which skills exist and what each one requires.
/// </summary>
public interface ISkillTaxonomy
{
    bool Contains(string skillTag);

    /// <summary>
    ///     The skills that must be demonstrated before the given one (direct prerequisites only).
    /// </summary>
    /// <exception cref="Shared.Domain.Exceptions.DomainException">Thrown when the skill is not in the taxonomy.</exception>
    IReadOnlyList<string> PrerequisitesOf(string skillTag);
    
    /// <summary>
    ///     The display name of a skill, or the tag itself when the skill is not in the taxonomy (so a path
    ///     saved before a catalog edit can still be shown).
    /// </summary>
    string NameOf(string skillTag);
}