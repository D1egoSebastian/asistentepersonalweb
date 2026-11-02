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
    private const int LogWindowDays = 14;

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
        var alerts = await LoadHabitAlertsAsync(today);
        return ideas.Count + alerts.Count == 0
            ? Skipped
            : await ReserveAndSendAsync(owner, today, utcNow, ideas, alerts);
    }

    // Active ideas that are due, oldest first. The last reminder of each idea comes from ReminderLog.
    private async Task<IReadOnlyList<(int Id, StaleIdea Idea)>> LoadStaleIdeasAsync(DateOnly today, TimeZoneInfo ownerZone)
    {
        var ideas = await db.Ideas
            .Where(i => i.UserId == User.OwnerId && i.ArchivedAt == null)
            .Select(i => new
            {
                i.Id,
                i.Text,
                i.UpdatedAt,
                LastRemindedOn = db.ReminderLogs.Where(r => r.IdeaId == i.Id).Max(r => (DateOnly?)r.SentOn),
            })
            .ToListAsync();

        return ideas
            .Select(i => (i.Id, i.Text, i.LastRemindedOn, UpdatedOn: OwnerClock.TodayAt(i.UpdatedAt, ownerZone)))
            .Where(i => ReminderRules.IsIdeaDue(i.UpdatedOn, i.LastRemindedOn, today))
            .OrderBy(i => i.UpdatedOn)
            .ThenBy(i => i.Id)
            .Select(i => (i.Id, new StaleIdea(i.Text, ReminderRules.DaysSince(i.UpdatedOn, today))))
            .ToList();
    }

    // The alerts of every habit of the owner. Only the logs of the last 14 days are loaded: the rules look back
    // to last Monday at most (plan D-T10). A streak longer than the window is counted up to the window.
    private async Task<IReadOnlyList<HabitAlert>> LoadHabitAlertsAsync(DateOnly today)
    {
        var since = today.AddDays(-LogWindowDays);
        var habits = await db.Habits.Where(h => h.UserId == User.OwnerId).ToListAsync();
        var logs = await db.HabitLogs
            .Where(l => l.Date >= since && db.Habits.Any(h => h.Id == l.HabitId && h.UserId == User.OwnerId))
            .Select(l => new { l.HabitId, l.Date })
            .ToListAsync();

        var datesByHabit = logs.ToLookup(l => l.HabitId, l => l.Date);
        return habits
            .SelectMany(h => ReminderRules.HabitAlerts(h, datesByHabit[h.Id].ToHashSet(), today))
            .ToList();
    }

    // Writes the rows first and sends second, all in one transaction: the day is only marked as sent if the email was.
    private async Task<ReminderOutcome> ReserveAndSendAsync(
        User owner, DateOnly today, DateTimeOffset utcNow,
        IReadOnlyList<(int Id, StaleIdea Idea)> ideas, IReadOnlyList<HabitAlert> alerts)
    {
        var habitCount = alerts.Select(a => a.HabitName).Distinct().Count();
        await using var transaction = await db.Database.BeginTransactionAsync();
        db.DailyDigestLogs.Add(new DailyDigestLog { ForDate = today, SentAt = utcNow });
        db.ReminderLogs.AddRange(ideas.Select(i => new ReminderLog { IdeaId = i.Id, SentOn = today }));
        var items = ideas.Count + habitCount;
        var rejected = await TrySaveAsync(items);
        if (rejected is not null)
        {
            return rejected;
        }

        var message = BuildMessage(owner, today, ideas.Select(i => i.Idea).ToList(), alerts, habitCount);
        if (!await email.SendAsync(message, CancellationToken.None))
        {
            // The rows are gone from the database; forget them here too so a new run can insert the same keys.
            await transaction.RollbackAsync();
            db.ChangeTracker.Clear();
            return new ReminderOutcome(ReminderResult.Failed, items);
        }

        await transaction.CommitAsync();
        return new ReminderOutcome(ReminderResult.Sent, items);
    }

    private static EmailMessage BuildMessage(
        User owner, DateOnly today, IReadOnlyList<StaleIdea> ideas, IReadOnlyList<HabitAlert> alerts, int habitCount) =>
        new(owner.Email,
            ReminderText.Subject(ideas.Count, habitCount),
            ReminderText.Body(ideas, alerts),
            $"digest-{today:yyyy-MM-dd}");

    // Null when the rows were saved. Skipped when another run already saved the digest of this day: the unique key
    // rejected ours (RNF-3). Failed when an idea was deleted after we loaded it: nothing is stored and the next run
    // goes on without it.
    private async Task<ReminderOutcome?> TrySaveAsync(int items)
    {
        try
        {
            await db.SaveChangesAsync();
            return null;
        }
        catch (DbUpdateException ex) when (DatabaseErrors.IsUniqueViolation(ex))
        {
            db.ChangeTracker.Clear();
            return Skipped;
        }
        catch (DbUpdateException ex) when (DatabaseErrors.IsForeignKeyViolation(ex))
        {
            db.ChangeTracker.Clear();
            return new ReminderOutcome(ReminderResult.Failed, items);
        }
    }
}
