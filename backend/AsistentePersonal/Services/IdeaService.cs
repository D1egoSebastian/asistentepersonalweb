using AsistentePersonal.Data;
using AsistentePersonal.Models;
using Microsoft.EntityFrameworkCore;

namespace AsistentePersonal.Services;

// Use cases for ideas. Every method takes the owner (userId). The caller decides "now" (plan D-T11).
public class IdeaService(AppDbContext db)
{
    public const string NotFoundMessage = "Esta idea ya no existe.";
    public const string ArchivedMessage = "Esta idea está archivada. Restáurala para editarla.";
    public const string ChangedMessage = "Esta idea cambió mientras la editabas. Esta es la versión nueva.";
    private const string ChangedElsewhereMessage = "Esta idea cambió en otra pestaña. Vuelve a intentarlo.";
    private const string LikeEscape = "\\";

    public async Task<ServiceResult<Idea>> CreateAsync(
        int userId, string? text, IReadOnlyList<string>? tags, DateTimeOffset now)
    {
        var normalizedTags = NormalizeTags(tags);
        var error = IdeaRules.ValidateText(text) ?? IdeaRules.ValidateTags(normalizedTags);
        if (error is not null)
        {
            return ServiceResult<Idea>.Invalid(error);
        }

        var idea = new Idea
        {
            UserId = userId,
            Text = IdeaRules.NormalizeText(text)!,
            Tags = (await UseExistingSpellingAsync(userId, normalizedTags)).ToList(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Ideas.Add(idea);
        await db.SaveChangesAsync();
        return ServiceResult<Idea>.Ok(idea);
    }

    public async Task<ServiceResult<Idea>> GetAsync(int userId, int id)
    {
        var idea = await FindOwnedAsync(userId, id);
        return idea is null
            ? ServiceResult<Idea>.NotFound(NotFoundMessage)
            : ServiceResult<Idea>.Ok(idea);
    }

    // Lists one section (active or archived), filtered by the search and by a tag, newest first (spec CA-3.1).
    public async Task<IReadOnlyList<IdeaListItem>> ListAsync(
        int userId, bool archived, string? search, string? tag, TimeZoneInfo ownerZone)
    {
        var query = db.Ideas.Where(i => i.UserId == userId && (archived ? i.ArchivedAt != null : i.ArchivedAt == null));
        foreach (var word in IdeaRules.SearchWords(search))
        {
            query = WhereWordMatches(query, word);
        }

        var cleanTag = IdeaRules.NormalizeTag(tag).ToLowerInvariant();
        if (cleanTag.Length > 0)
        {
            query = query.Where(i => i.Tags.Any(t => t.ToLower() == cleanTag));
        }

        var ideas = await query
            .OrderByDescending(i => i.CreatedAt)
            .ThenByDescending(i => i.Id)
            .ToListAsync();

        return ideas
            .Select(i => new IdeaListItem(
                i.Id,
                IdeaRules.Summary(i.Text),
                i.Tags,
                OwnerClock.TodayAt(i.CreatedAt, ownerZone),
                OwnerClock.TodayAt(i.UpdatedAt, ownerZone)))
            .ToList();
    }

    // Edits an idea. "version" is the version the owner opened: if the idea changed since, the edit is refused.
    public async Task<ServiceResult<Idea>> UpdateAsync(
        int userId, int id, string? text, IReadOnlyList<string>? tags, uint version, DateTimeOffset now)
    {
        var idea = await FindOwnedAsync(userId, id);
        if (idea is null)
        {
            return ServiceResult<Idea>.NotFound(NotFoundMessage);
        }

        var refusal = RefuseEdit(idea, version);
        if (refusal is not null)
        {
            return refusal;
        }

        return await ApplyEditAsync(userId, idea, text, tags, now);
    }

    // Validates the new text and tags, and saves them only if something changed (spec CA-5.3).
    private async Task<ServiceResult<Idea>> ApplyEditAsync(
        int userId, Idea idea, string? text, IReadOnlyList<string>? tags, DateTimeOffset now)
    {
        var normalizedTags = NormalizeTags(tags);
        var error = IdeaRules.ValidateText(text) ?? IdeaRules.ValidateTags(normalizedTags);
        if (error is not null)
        {
            return ServiceResult<Idea>.Invalid(error);
        }

        var newText = IdeaRules.NormalizeText(text)!;
        var newTags = await UseExistingSpellingAsync(userId, normalizedTags);
        if (newText == idea.Text && newTags.SequenceEqual(idea.Tags))
        {
            return ServiceResult<Idea>.Ok(idea);
        }

        idea.Text = newText;
        idea.Tags = newTags.ToList();
        idea.UpdatedAt = now;
        return ToResult(await SaveAsync(userId, idea.Id, ChangedMessage), idea);
    }

    // An archived idea is read-only, and an idea that changed since the owner opened it must not be overwritten.
    private static ServiceResult<Idea>? RefuseEdit(Idea idea, uint version)
    {
        if (idea.ArchivedAt is not null)
        {
            return ServiceResult<Idea>.Conflict(ArchivedMessage);
        }

        return idea.Version != version ? ServiceResult<Idea>.Conflict(ChangedMessage) : null;
    }

    public async Task<ServiceResult> ArchiveAsync(int userId, int id, DateTimeOffset now)
    {
        var idea = await FindOwnedAsync(userId, id);
        if (idea is null)
        {
            return ServiceResult.NotFound(NotFoundMessage);
        }

        if (idea.ArchivedAt is not null)
        {
            return ServiceResult.Ok();
        }

        idea.ArchivedAt = now;
        return await SaveAsync(userId, id, ChangedElsewhereMessage);
    }

    // Puts the idea back in the active list. Its text, tags and creation date stay as they were.
    // UpdatedAt is reset to now: the 14-day reminder clock starts again (spec 003 CA-6.3, spec 005 S-3).
    public async Task<ServiceResult> RestoreAsync(int userId, int id, DateTimeOffset now)
    {
        var idea = await FindOwnedAsync(userId, id);
        if (idea is null)
        {
            return ServiceResult.NotFound(NotFoundMessage);
        }

        if (idea.ArchivedAt is null)
        {
            return ServiceResult.Ok();
        }

        idea.ArchivedAt = null;
        idea.UpdatedAt = now;
        return await SaveAsync(userId, id, ChangedElsewhereMessage);
    }

    public async Task<ServiceResult> DeleteAsync(int userId, int id)
    {
        var idea = await FindOwnedAsync(userId, id);
        if (idea is null)
        {
            return ServiceResult.NotFound(NotFoundMessage);
        }

        db.Ideas.Remove(idea);
        return await SaveAsync(userId, id, ChangedElsewhereMessage);
    }

    // Every tag of the owner with its number of active and archived ideas (spec CA-2.9, CA-4.6, CA-6.4).
    public async Task<IReadOnlyList<TagCount>> TagCountsAsync(int userId)
    {
        var uses = await LoadTagUsesAsync(userId);

        return uses
            .GroupBy(u => u.Key)
            .Select(group => new TagCount(
                MostUsedSpelling(group),
                group.Count(u => !u.Archived),
                group.Count(u => u.Archived)))
            .OrderBy(count => count.Name)
            .ToList();
    }

    // Empty and repeated tags are dropped or caught by the rules; the rest keep the owner's order.
    private static IReadOnlyList<string> NormalizeTags(IReadOnlyList<string>? tags) =>
        IdeaRules.DistinctTags((tags ?? Array.Empty<string>()).Select(IdeaRules.NormalizeTag));

    // A tag that already exists is saved with the spelling the owner uses most (spec CA-2.2, D-T5).
    private async Task<IReadOnlyList<string>> UseExistingSpellingAsync(int userId, IReadOnlyList<string> tags)
    {
        var uses = await LoadTagUsesAsync(userId);
        var spellings = uses
            .GroupBy(u => u.Key)
            .ToDictionary(group => group.Key, MostUsedSpelling);

        return tags
            .Select(tag => spellings.TryGetValue(tag.ToLowerInvariant(), out var spelling) ? spelling : tag)
            .ToList();
    }

    private async Task<List<(string Key, string Spelling, bool Archived)>> LoadTagUsesAsync(int userId)
    {
        var ideas = await db.Ideas
            .Where(i => i.UserId == userId)
            .Select(i => new { i.Tags, Archived = i.ArchivedAt != null })
            .ToListAsync();

        return ideas
            .SelectMany(i => i.Tags.Select(tag => (Key: tag.ToLowerInvariant(), Spelling: tag, i.Archived)))
            .ToList();
    }

    private static string MostUsedSpelling(IEnumerable<(string Key, string Spelling, bool Archived)> uses) =>
        uses.GroupBy(u => u.Spelling)
            .OrderByDescending(group => group.Count())
            .First()
            .Key;

    private static IQueryable<Idea> WhereWordMatches(IQueryable<Idea> query, string word)
    {
        // The escape character is passed explicitly: without it EF Core emits ESCAPE '' and "%" would match everything.
        var pattern = IdeaRules.LikePattern(word);
        return query.Where(i =>
            EF.Functions.ILike(IdeaFunctions.Unaccent(i.Text), IdeaFunctions.Unaccent(pattern), LikeEscape)
            || i.Tags.Any(t => EF.Functions.ILike(IdeaFunctions.Unaccent(t), IdeaFunctions.Unaccent(pattern), LikeEscape)));
    }

    private Task<Idea?> FindOwnedAsync(int userId, int id) =>
        db.Ideas.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

    // Saves the change. Another request may have changed or deleted the idea since we read it:
    // the database then updates no row (xmin changed) and EF reports a concurrency error.
    private async Task<ServiceResult> SaveAsync(int userId, int id, string changedMessage)
    {
        try
        {
            await db.SaveChangesAsync();
            return ServiceResult.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            var stillExists = await db.Ideas.AnyAsync(i => i.Id == id && i.UserId == userId);
            return stillExists
                ? ServiceResult.Conflict(changedMessage)
                : ServiceResult.NotFound(NotFoundMessage);
        }
    }

    private static ServiceResult<Idea> ToResult(ServiceResult saved, Idea idea) =>
        saved.Status == ResultStatus.Ok
            ? ServiceResult<Idea>.Ok(idea)
            : new ServiceResult<Idea>(saved.Status, default, saved.Message);
}
