using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SkillSwap.Platform.LearningPathEngine.Infrastructure.Taxonomy;

/// <summary>
///     Normalizes text so that keywords and goals are compared regardless of case, accents or punctuation:
///     lowercase, no diacritics, and every character other than letters, digits, '#' and '+' becomes a space
///     (so "C#" and "ASP.NET" keep meaning).
/// </summary>
internal static partial class TextNormalizer
{
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var withoutAccents = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                withoutAccents.Append(character);

        return NonSearchableCharacters().Replace(withoutAccents.ToString(), " ").Trim();
    }

    [GeneratedRegex("[^a-z0-9#+]+")]
    private static partial Regex NonSearchableCharacters();
}