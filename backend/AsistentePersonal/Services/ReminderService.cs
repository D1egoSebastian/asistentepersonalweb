using AsistentePersonal.Data;
using AsistentePersonal.Models;
using Microsoft.EntityFrameworkCore;

namespace AsistentePersonal.Services;

public enum ReminderResult
{
    Sent,
    Skipped,
    Failed,
}

// What a run of the daily digest did, and how many ideas and habits it included.
public sealed record ReminderOutcome(ReminderResult Result, int Items);

// The daily digest: finds what needs attention, reserves the day in the database and sends one email.
// The caller decides "now" (plan D-T2); the service never reads the clock.
public class ReminderService(AppDbContext db, IEmailSender email)
{
    private static readonly ReminderOutcome Skipped = new(ReminderResult.Skipped, 0);

    public async Task<ReminderOutcome> SendDailyAsync(DateTimeOffset utcNow, TimeZoneInfo ownerZone)
    {
        if (!ReminderRules.IsDigestTime(OwnerClock.LocalTimeAt(utcNow, ownerZone)))
        {
            return Skipped;
        }

        var today = OwnerClock.TodayAt(utcNow, ownerZone);
        var owner = await db.Users.FindAsync(User.OwnerId);
        if (owner is null || await db.DailyDigestLogs.AnyAsync(d => d.ForDate == today))
        {
            return Skipped;
        }

        var ideas = await LoadStaleIdeasAsync(today, ownerZone);
        return ideas.Count == 0 ? Skipped : await ReserveAndSendAsync(owner, today, utcNow, ideas);
    }

    private async Task<IReadOnlyList<(int Id, StaleIdea Idea)>> LoadStaleIdeasAsync(DateOnly today, TimeZoneInfo ownerZone)
    {
        var ideas = await db.Ideas
            .Where(i => i.UserId == User.OwnerId && i.ArchivedAt == null)
            .Select(i => new { i.Id, i.Text, i.UpdatedAt })
            .ToListAsync();

        return ideas
            .Select(i => (i.Id, UpdatedOn: OwnerClock.TodayAt(i.UpdatedAt, ownerZone), i.Text))
            .Where(i => ReminderRules.IsIdeaDue(i.UpdatedOn, null, today))
            .Select(i => (i.Id, new StaleIdea(i.Text, ReminderRules.DaysSince(i.UpdatedOn, today))))
            .ToList();
    }

    // Writes the rows first and sends second, all in one transaction: the day is only marked as sent if the email was.
    private async Task<ReminderOutcome> ReserveAndSendAsync(
        User owner, DateOnly today, DateTimeOffset utcNow, IReadOnlyList<(int Id, StaleIdea Idea)> ideas)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        db.DailyDigestLogs.Add(new DailyDigestLog { ForDate = today, SentAt = utcNow });
        db.ReminderLogs.AddRange(ideas.Select(i => new ReminderLog { IdeaId = i.Id, SentOn = today }));
        await db.SaveChangesAsync();

        var message = new EmailMessage(
            owner.Email,
            ReminderText.Subject(ideas.Count, 0),
            ReminderText.Body(ideas.Select(i => i.Idea).ToList(), []),
            $"digest-{today:yyyy-MM-dd}");
        await email.SendAsync(message, CancellationToken.None);

        await transaction.CommitAsync();
        return new ReminderOutcome(ReminderResult.Sent, ideas.Count);
    }
}
