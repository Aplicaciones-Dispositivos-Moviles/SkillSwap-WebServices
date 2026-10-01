using SkillSwap.Platform.LearningPathEngine.Domain.Model.Entities;
using SkillSwap.Platform.LearningPathEngine.Domain.Model.ValueObjects;
using SkillSwap.Platform.Shared.Domain.Exceptions;

namespace SkillSwap.Platform.LearningPathEngine.Domain.Services;

/// <summary>
///     Orders the missing skills topologically, level by level (alphabetically inside a level, so the
///     result is deterministic). Nodes without pending prerequisites start available; the rest, locked.
/// </summary>
/// <param name="taxonomy">The skill taxonomy</param>
public class LearningPathBuilder(ISkillTaxonomy taxonomy) : ILearningPathBuilder
{
    /// <inheritdoc />
    public IReadOnlyList<PathNode> BuildPath(SkillGap gap)
    {
        var missing = new HashSet<string>(gap.MissingSkillTags, StringComparer.Ordinal);

        // Only prerequisites that are part of this path matter: the others were already demonstrated.
        var inPathPrerequisites = missing.ToDictionary(
            tag => tag,
            tag => taxonomy.PrerequisitesOf(tag).Where(missing.Contains).ToList(),
            StringComparer.Ordinal);

        var ordered = new List<string>();
        var placed = new HashSet<string>(StringComparer.Ordinal);
        while (placed.Count < missing.Count)
        {
            var ready = missing
                .Where(tag => !placed.Contains(tag) && inPathPrerequisites[tag].All(placed.Contains))
                .OrderBy(tag => tag, StringComparer.Ordinal)
                .ToList();
            if (ready.Count == 0)
                throw new DomainException("The skill taxonomy contains a prerequisite cycle.");

            ordered.AddRange(ready);
            foreach (var tag in ready) placed.Add(tag);
        }

        return ordered
            .Select((tag, index) => new PathNode(tag, index + 1, inPathPrerequisites[tag]))
            .ToList();
    }
}