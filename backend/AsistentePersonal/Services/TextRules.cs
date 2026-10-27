using System.Globalization;

namespace AsistentePersonal.Services;

// Pure text rules shared by habits, login and ideas. No database, no clock.
public static class TextRules
{
    // True when there is nothing the user can see: only spaces or invisible characters.
    public static bool IsBlank(string? text)
    {
        if (text is null)
        {
            return true;
        }

        return text.All(c => char.IsWhiteSpace(c) || char.GetUnicodeCategory(c) == UnicodeCategory.Format);
    }

    // Trims the text. Returns null when it is blank, so the caller can store "no value".
    public static string? Normalize(string? text)
    {
        if (IsBlank(text))
        {
            return null;
        }

        return text!.Trim();
    }

    // Counts what the user sees as characters (an emoji or a letter with an accent is 1).
    public static int CountChars(string text)
    {
        return new StringInfo(text).LengthInTextElements;
    }
}
