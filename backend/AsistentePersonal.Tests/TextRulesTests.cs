using AsistentePersonal.Services;

namespace AsistentePersonal.Tests;

public class TextRulesTests
{
    // ---- T08: text helpers ----

    [Fact]
    public void Normalize_TrimsSpacesAtStartAndEnd()
    {
        Assert.Equal("Leer", TextRules.Normalize("  Leer  "));
    }

    [Fact]
    public void Normalize_ReturnsNull_WhenTextIsOnlySpaces()
    {
        Assert.Null(TextRules.Normalize("   "));
    }

    [Fact]
    public void Normalize_ReturnsNull_WhenTextIsNull()
    {
        Assert.Null(TextRules.Normalize(null));
    }

    [Fact]
    public void Normalize_ReturnsNull_WhenTextIsOnlyInvisibleCharacters()
    {
        // U+200B (zero width space) and U+200D (zero width joiner) are invisible
        Assert.Null(TextRules.Normalize("​‍"));
    }

    [Fact]
    public void IsBlank_TrueForInvisibleCharacters()
    {
        Assert.True(TextRules.IsBlank("​ ‍"));
    }

    [Fact]
    public void IsBlank_FalseWhenTextHasVisibleCharacters()
    {
        Assert.False(TextRules.IsBlank(" a "));
    }

    [Fact]
    public void CountChars_CountsAsciiLettersOneByOne()
    {
        Assert.Equal(3, TextRules.CountChars("abc"));
    }

    [Fact]
    public void CountChars_CountsEmojiAsOneCharacter()
    {
        // U+1F600 is a single emoji, stored as two UTF-16 code units
        Assert.Equal(1, TextRules.CountChars("\U0001F600"));
    }

    [Fact]
    public void CountChars_CountsLetterWithAccentAsOneCharacter()
    {
        // "e" followed by a combining acute accent: what the user sees is one letter
        Assert.Equal(1, TextRules.CountChars("é"));
    }
}
