using AsistentePersonal.Services;

namespace AsistentePersonal.Tests;

public class IdeaRulesTests
{
    // ---- T04: idea text ----

    [Fact]
    public void ValidateText_ReturnsError_WhenEmpty()
    {
        Assert.NotNull(IdeaRules.ValidateText(""));
        Assert.NotNull(IdeaRules.ValidateText(null));
    }

    [Fact]
    public void ValidateText_ReturnsError_WhenOnlyLineBreaksAndSpaces()
    {
        Assert.NotNull(IdeaRules.ValidateText("\n\n  \r\n"));
    }

    [Fact]
    public void ValidateText_Accepts5000Characters()
    {
        Assert.Null(IdeaRules.ValidateText(new string('a', 5000)));
    }

    [Fact]
    public void ValidateText_Rejects5001Characters()
    {
        Assert.NotNull(IdeaRules.ValidateText(new string('a', 5001)));
    }

    [Fact]
    public void NormalizeText_TrimsTheEdgesAndKeepsInnerLineBreaks()
    {
        Assert.Equal("Linea 1\nLinea 2", IdeaRules.NormalizeText("  \nLinea 1\nLinea 2 \n "));
    }

    [Fact]
    public void NormalizeText_ReturnsNull_WhenBlank()
    {
        Assert.Null(IdeaRules.NormalizeText(" \n\t "));
    }

    // ---- T05: tags ----

    [Fact]
    public void NormalizeTag_RemovesLeadingHashAndSurroundingSpaces()
    {
        Assert.Equal("App", IdeaRules.NormalizeTag("  #App "));
        Assert.Equal("App", IdeaRules.NormalizeTag("#  App"));
    }

    [Fact]
    public void ValidateTags_AcceptsNull_AsNoTags()
    {
        Assert.Null(IdeaRules.ValidateTags(null));
    }

    [Fact]
    public void ValidateTags_RejectsEmptyTag()
    {
        Assert.NotNull(IdeaRules.ValidateTags(new[] { "" }));
    }

    [Fact]
    public void ValidateTags_RejectsTagWithComma()
    {
        Assert.NotNull(IdeaRules.ValidateTags(new[] { "libros,cine" }));
    }

    [Fact]
    public void ValidateTags_Accepts30Characters()
    {
        Assert.Null(IdeaRules.ValidateTags(new[] { new string('a', 30) }));
    }

    [Fact]
    public void ValidateTags_Rejects31Characters()
    {
        Assert.NotNull(IdeaRules.ValidateTags(new[] { new string('a', 31) }));
    }

    [Fact]
    public void ValidateTags_Accepts10Tags()
    {
        var tags = Enumerable.Range(1, 10).Select(i => $"etiqueta{i}").ToList();
        Assert.Null(IdeaRules.ValidateTags(tags));
    }

    [Fact]
    public void ValidateTags_Rejects11Tags()
    {
        var tags = Enumerable.Range(1, 11).Select(i => $"etiqueta{i}").ToList();
        Assert.NotNull(IdeaRules.ValidateTags(tags));
    }

    [Fact]
    public void DistinctTags_KeepsTheFirstSpellingOfRepeatedTagsIgnoringCase()
    {
        var result = IdeaRules.DistinctTags(new[] { "app", "App", "lluvia" });

        Assert.Equal(new[] { "app", "lluvia" }, result);
    }

    // ---- T06: summary and search ----

    [Fact]
    public void Summary_UsesTheFirstLineThatHasText()
    {
        Assert.Equal("Segunda", IdeaRules.Summary("\n  \nSegunda"));
        Assert.Equal("Primera", IdeaRules.Summary("Primera\nSegunda"));
    }

    [Fact]
    public void Summary_Keeps120Characters()
    {
        var text = new string('a', 120);
        Assert.Equal(text, IdeaRules.Summary(text));
    }

    [Fact]
    public void Summary_CutsAt120CharactersWithAnEllipsis()
    {
        Assert.Equal(new string('a', 120) + "…", IdeaRules.Summary(new string('a', 121)));
    }

    [Fact]
    public void Summary_DoesNotSplitAnEmojiAtTheCut()
    {
        // 119 letters, then an emoji (one visible character, two UTF-16 units), then more text.
        var text = new string('a', 119) + "\U0001F600" + "bbb";

        Assert.Equal(new string('a', 119) + "\U0001F600" + "…", IdeaRules.Summary(text));
    }

    [Fact]
    public void LikePattern_EscapesPercentUnderscoreAndBackslash()
    {
        Assert.Equal("%a\\%b\\_c\\\\d%", IdeaRules.LikePattern("a%b_c\\d"));
    }

    [Fact]
    public void SearchWords_SplitsOnOneOrMoreSpaces()
    {
        Assert.Equal(new[] { "sueño", "app" }, IdeaRules.SearchWords("sueño   app"));
    }

    [Fact]
    public void SearchWords_IsEmpty_WhenOnlySpaces()
    {
        Assert.Empty(IdeaRules.SearchWords("   "));
    }
}
