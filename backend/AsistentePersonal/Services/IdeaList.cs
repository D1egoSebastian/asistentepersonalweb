namespace AsistentePersonal.Services;

// One idea as the list shows it: the summary, not the full text (plan D-T3).
public sealed record IdeaListItem(int Id, string Summary, IReadOnlyList<string> Tags, DateOnly CreatedDate, DateOnly UpdatedDate);

// How many ideas use a tag in each section. Spellings that differ only in case count as one tag (spec CA-2.11).
public sealed record TagCount(string Name, int ActiveCount, int ArchivedCount);
