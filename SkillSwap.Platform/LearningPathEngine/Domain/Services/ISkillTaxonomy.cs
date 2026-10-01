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
}