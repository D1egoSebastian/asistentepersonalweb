namespace AsistentePersonal.DTOs;

// The text and tags the owner sends. Version is the version the owner opened; editing requires it (spec CA-5.5).
public sealed record IdeaRequest(string? Text, List<string>? Tags, uint? Version);

// One row of the list: the summary, not the full text (plan D-T3).
public sealed record IdeaSummaryResponse(int Id, string Summary, IReadOnlyList<string> Tags, DateOnly CreatedDate);

public sealed record IdeaListResponse(IReadOnlyList<IdeaSummaryResponse> Ideas);

// One idea in full. "version" lets the next edit detect changes made in another tab.
public sealed record IdeaResponse(
    int Id,
    string Text,
    IReadOnlyList<string> Tags,
    DateOnly CreatedDate,
    bool Archived,
    uint Version);

public sealed record TagCountResponse(string Name, int ActiveCount, int ArchivedCount);
