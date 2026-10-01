using SkillSwap.Platform.LearningPathEngine.Domain.Services;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.LearningPathEngine.Infrastructure.Taxonomy;

/// <summary>
///     <see cref="ISkillTaxonomy" /> backed by the JSON skill catalog
/// </summary>
/// <param name="catalog">The loaded catalog</param>
public class JsonSkillTaxonomy(SkillCatalog catalog) : ISkillTaxonomy
{
    /// <inheritdoc />
    public bool Contains(string skillTag)
    {
        return catalog.TryGet(skillTag, out _);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> PrerequisitesOf(string skillTag)
    {
        return catalog.TryGet(skillTag, out var skill)
            ? skill.Prerequisites
            : throw new DomainException($"The skill '{skillTag}' is not in the taxonomy.");
    }
    
    /// <inheritdoc />
    public string NameOf(string skillTag)
    {
        return catalog.TryGet(skillTag, out var skill) ? skill.Name : skillTag;
    }
}