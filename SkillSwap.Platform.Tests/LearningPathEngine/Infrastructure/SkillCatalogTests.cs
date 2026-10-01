using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SkillSwap.Platform.Tests.LearningPathEngine.Infrastructure;

/// <summary>
///     Guards the integrity of the skill catalog file: any edit that breaks the taxonomy fails here.
/// </summary>
public class SkillCatalogTests
{
    private static readonly string[] AllowedCategories =
        ["fundamentals", "web", "backend", "devops", "database", "data", "design", "mobile"];

    /// <summary>
    ///     Keywords too ambiguous to identify a skill on their own (compared after normalization).
    /// </summary>
    private static readonly HashSet<string> ForbiddenKeywords =
    [
        "seguridad", "security", "excel", "nube", "cloud", "permisos", "room", "offline", "sprint", "agile",
        "agil", "testing", "solid", "terminal", "shell", "spring", "node", "express", "contenedores",
        "containers", "deploy", "deployment", "despliegue", "localizacion", "persistencia", "commits",
        "dashboards", "widgets", "multiplataforma", "dio", "qa", "aria", "backup", "respaldo", "indices",
        "triggers", "normalizacion", "camara", "sensores", "gps"
    ];

    private static readonly List<Skill> Skills = Load();

    private sealed record Skill(string Tag, string Name, string Category, List<string> Keywords,
        List<string> Prerequisites);

    private static List<Skill> Load()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "SkillSwap.Platform", "LearningPathEngine",
                "Infrastructure", "Taxonomy", "skill-catalog.json");
            if (File.Exists(candidate))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(candidate));
                return document.RootElement.GetProperty("skills").EnumerateArray().Select(skill => new Skill(
                    skill.GetProperty("tag").GetString()!,
                    skill.GetProperty("name").GetString()!,
                    skill.GetProperty("category").GetString()!,
                    skill.GetProperty("keywords").EnumerateArray().Select(k => k.GetString()!).ToList(),
                    skill.GetProperty("prerequisites").EnumerateArray().Select(p => p.GetString()!).ToList())).ToList();
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "skill-catalog.json was not found under SkillSwap.Platform/LearningPathEngine/Infrastructure/Taxonomy.");
    }

    /// <summary>
    ///     Lowercase, no accents, and every character other than letters, digits, '#' and '+' becomes a space.
    /// </summary>
    private static string Normalize(string text)
    {
        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var withoutAccents = new string(decomposed
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        return Regex.Replace(Regex.Replace(withoutAccents, "[^a-z0-9#+]", " "), @"\s+", " ").Trim();
    }

    [Fact]
    public void Catalog_IsNotEmpty()
    {
        Assert.True(Skills.Count >= 40);
    }

    [Fact]
    public void Tags_AreUniqueAndLowercaseKebabCase()
    {
        Assert.Equal(Skills.Count, Skills.Select(s => s.Tag).Distinct().Count());
        Assert.All(Skills, s => Assert.Matches("^[a-z0-9]+(-[a-z0-9]+)*$", s.Tag));
    }

    [Fact]
    public void EverySkill_HasNameAndAnAllowedCategory()
    {
        Assert.All(Skills, s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.Name), s.Tag);
            Assert.Contains(s.Category, AllowedCategories);
        });
    }

    [Fact]
    public void Prerequisites_ExistAndAreNotTheSkillItself()
    {
        var tags = Skills.Select(s => s.Tag).ToHashSet();
        Assert.All(Skills, s =>
        {
            Assert.DoesNotContain(s.Tag, s.Prerequisites);
            Assert.All(s.Prerequisites, p => Assert.True(tags.Contains(p), $"{s.Tag} -> {p}"));
            Assert.Equal(s.Prerequisites.Count, s.Prerequisites.Distinct().Count());
        });
    }

    [Fact]
    public void Prerequisites_FormNoCycle()
    {
        var byTag = Skills.ToDictionary(s => s.Tag);
        var state = new Dictionary<string, int>();

        void Visit(string tag, string trail)
        {
            if (state.GetValueOrDefault(tag) == 2) return;
            Assert.False(state.GetValueOrDefault(tag) == 1, $"Cycle: {trail} -> {tag}");
            state[tag] = 1;
            foreach (var prerequisite in byTag[tag].Prerequisites) Visit(prerequisite, $"{trail} -> {tag}");
            state[tag] = 2;
        }

        foreach (var skill in Skills) Visit(skill.Tag, string.Empty);
    }

    [Fact]
    public void EverySkill_HasKeywords_AndNoneIsBlankAfterNormalization()
    {
        Assert.All(Skills, s =>
        {
            Assert.NotEmpty(s.Keywords);
            Assert.All(s.Keywords, k => Assert.False(string.IsNullOrWhiteSpace(Normalize(k)), $"{s.Tag}: '{k}'"));
            Assert.Equal(s.Keywords.Count, s.Keywords.Select(Normalize).Distinct().Count());
        });
    }

    [Fact]
    public void Keywords_AreNotSharedBetweenSkills()
    {
        var shared = Skills
            .SelectMany(s => s.Keywords.Select(k => (Keyword: Normalize(k), s.Tag)))
            .GroupBy(x => x.Keyword)
            .Where(g => g.Select(x => x.Tag).Distinct().Count() > 1)
            .Select(g => $"'{g.Key}' in {string.Join(", ", g.Select(x => x.Tag).Distinct())}")
            .ToList();

        Assert.Empty(shared);
    }

    [Fact]
    public void Keywords_DoNotIncludeTooAmbiguousWords()
    {
        var forbidden = Skills
            .SelectMany(s => s.Keywords.Select(k => (s.Tag, Keyword: k)))
            .Where(x => ForbiddenKeywords.Contains(Normalize(x.Keyword)))
            .Select(x => $"{x.Tag}: '{x.Keyword}'")
            .ToList();

        Assert.Empty(forbidden);
    }
}