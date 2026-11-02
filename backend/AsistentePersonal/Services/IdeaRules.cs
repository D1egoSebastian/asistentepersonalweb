using System.Globalization;

namespace AsistentePersonal.Services;

// Pure rules for ideas. No database, no clock. Each Validate* method returns null when the input is valid,
// or a message in Spanish when it is not.
public static class IdeaRules
{
    public const int MaxTextLength = 5000;
    public const int MaxTagLength = 30;
    public const int MaxTagsPerIdea = 10;
    public const int SummaryLength = 120;

    // Trims the edges and keeps the line breaks inside. Returns null when there is no visible text.
    public static string? NormalizeText(string? text) => TextRules.Normalize(text);

    public static string? ValidateText(string? text)
    {
        var normalized = NormalizeText(text);
        if (normalized is null)
        {
            return "La idea no puede estar vacía.";
        }

        if (TextRules.CountChars(normalized) > MaxTextLength)
        {
            return $"La idea no puede tener más de {MaxTextLength} caracteres.";
        }

        return null;
    }

    // Trims the spaces and removes one leading "#": "#App" becomes "App".
    public static string NormalizeTag(string? tag)
    {
        var trimmed = (tag ?? "").Trim();
        if (trimmed.StartsWith('#'))
        {
            trimmed = trimmed[1..].Trim();
        }

        return trimmed;
    }

    // A null list means the idea has no tags. Each tag is expected to be normalized already.
    public static string? ValidateTags(IReadOnlyList<string>? tags)
    {
        if (tags is null)
        {
            return null;
        }

        if (tags.Count > MaxTagsPerIdea)
        {
            return $"Una idea puede tener hasta {MaxTagsPerIdea} etiquetas.";
        }

        return tags.Select(ValidateTag).FirstOrDefault(error => error is not null);
    }

    // Removes repeated tags ignoring case. The first spelling wins: "app" then "App" keeps "app".
    public static IReadOnlyList<string> DistinctTags(IEnumerable<string> tags)
    {
        var seen = new HashSet<string>();
        var result = new List<string>();
        foreach (var tag in tags)
        {
            if (seen.Add(tag.ToLowerInvariant()))
            {
                result.Add(tag);
            }
        }

        return result;
    }

    // The first line that has text, cut to 120 visible characters with "…" when it is longer.
    public static string Summary(string? text) => Cut(text, SummaryLength);

    // The first line that has text, cut to maxChars visible characters with "…" when it is longer.
    // Visible characters are text elements, so an emoji or a letter with an accent is never split.
    public static string Cut(string? text, int maxChars)
    {
        var normalized = NormalizeText(text);
        if (normalized is null)
        {
            return "";
        }

        var firstLine = normalized.Split('\n').Select(TextRules.Normalize).First(line => line is not null)!;
        if (TextRules.CountChars(firstLine) <= maxChars)
        {
            return firstLine;
        }

        return new StringInfo(firstLine).SubstringByTextElements(0, maxChars) + "…";
    }

    // Words of a search, separated by one or more spaces. A blank search gives an empty list.
    public static IReadOnlyList<string> SearchWords(string? search) =>
        (search ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    // Matches the word anywhere in the text. The LIKE wildcards "%", "_" and "\" are escaped so they
    // are searched as plain characters (CA-4.5).
    public static string LikePattern(string word)
    {
        var escaped = word.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        return $"%{escaped}%";
    }

    private static string? ValidateTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return "Una etiqueta no puede estar vacía.";
        }

        if (tag.Contains(','))
        {
            return "Las etiquetas no pueden contener comas.";
        }

        if (TextRules.CountChars(tag) > MaxTagLength)
        {
            return $"Cada etiqueta puede tener hasta {MaxTagLength} caracteres.";
        }

        return null;
    }
}
