using Microsoft.EntityFrameworkCore;

namespace AsistentePersonal.Data;

// PostgreSQL's unaccent(text) so searches ignore accents (spec 003, CA-4.1). EF Core translates it to SQL;
// the C# body is never run.
public static class IdeaFunctions
{
    [DbFunction("unaccent")]
    public static string Unaccent(string text) => throw new NotSupportedException("Translated to SQL by EF Core.");
}
