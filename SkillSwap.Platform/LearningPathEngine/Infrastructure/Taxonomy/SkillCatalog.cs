using System.Text.Json;

namespace SkillSwap.Platform.LearningPathEngine.Infrastructure.Taxonomy;

public sealed record SkillDefinition(
    string Tag,
    string Name,
    string Category,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<string> Prerequisites);

/// <summary>
///     The internal skill taxonomy, loaded from the embedded skill-catalog.json. It is validated when
///     loaded, so an inconsistent catalog stops the application at startup instead of failing later.
/// </summary>
public sealed class SkillCatalog
{
    private const string ResourceName = "skill-catalog.json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Dictionary<string, SkillDefinition> _byTag;

    private SkillCatalog(IReadOnlyList<SkillDefinition> skills)
    {
        Skills = skills;
        _byTag = skills.ToDictionary(skill => skill.Tag, StringComparer.Ordinal);
    }

    public IReadOnlyList<SkillDefinition> Skills { get; }

    public bool TryGet(string tag, out SkillDefinition skill)
    {
        return _byTag.TryGetValue(tag, out skill!);
    }

    public static SkillCatalog LoadEmbedded()
    {
        using var stream = typeof(SkillCatalog).Assembly.GetManifestResourceStream(ResourceName)
                           ?? throw new InvalidOperationException(
                               $"The embedded resource '{ResourceName}' was not found. " +
                               "Check the EmbeddedResource entry in the project file.");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    public static SkillCatalog Parse(string json)
    {
        CatalogDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<CatalogDocument>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Invalid skill catalog: it is not valid JSON.", exception);
        }

        if (document?.Skills is null || document.Skills.Count == 0)
            throw Invalid("it has no skills");

        var skills = document.Skills.Select(entry => new SkillDefinition(
            entry.Tag?.Trim() ?? string.Empty,
            entry.Name?.Trim() ?? string.Empty,
            entry.Category?.Trim() ?? string.Empty,
            (entry.Keywords ?? []).ToList(),
            (entry.Prerequisites ?? []).ToList())).ToList();

        Validate(skills);
        return new SkillCatalog(skills);
    }

    private static void Validate(IReadOnlyList<SkillDefinition> skills)
    {
        var tags = new HashSet<string>(StringComparer.Ordinal);
        foreach (var skill in skills)
        {
            if (string.IsNullOrWhiteSpace(skill.Tag)) throw Invalid("a skill has no tag");
            if (!tags.Add(skill.Tag)) throw Invalid($"the tag '{skill.Tag}' is repeated");
            if (string.IsNullOrWhiteSpace(skill.Name)) throw Invalid($"the skill '{skill.Tag}' has no name");
            if (!skill.Keywords.Any(keyword => TextNormalizer.Normalize(keyword).Length > 0))
                throw Invalid($"the skill '{skill.Tag}' has no usable keyword");
        }

        foreach (var skill in skills)
        foreach (var prerequisite in skill.Prerequisites)
        {
            if (prerequisite == skill.Tag) throw Invalid($"the skill '{skill.Tag}' requires itself");
            if (!tags.Contains(prerequisite))
                throw Invalid($"the skill '{skill.Tag}' requires the unknown skill '{prerequisite}'");
        }

        var byTag = skills.ToDictionary(skill => skill.Tag, StringComparer.Ordinal);
        var state = new Dictionary<string, int>(StringComparer.Ordinal); // 1 = visiting, 2 = done

        void Visit(string tag, string trail)
        {
            if (state.GetValueOrDefault(tag) == 2) return;
            if (state.GetValueOrDefault(tag) == 1) throw Invalid($"there is a prerequisite cycle: {trail} -> {tag}");

            state[tag] = 1;
            foreach (var prerequisite in byTag[tag].Prerequisites) Visit(prerequisite, $"{trail} -> {tag}");
            state[tag] = 2;
        }

        foreach (var skill in skills) Visit(skill.Tag, "start");
    }

    private static InvalidOperationException Invalid(string reason)
    {
        return new InvalidOperationException($"Invalid skill catalog: {reason}.");
    }

    internal sealed record CatalogDocument(int Version, List<SkillEntry>? Skills);

    internal sealed record SkillEntry(
        string? Tag,
        string? Name,
        string? Category,
        List<string>? Keywords,
        List<string>? Prerequisites);
}