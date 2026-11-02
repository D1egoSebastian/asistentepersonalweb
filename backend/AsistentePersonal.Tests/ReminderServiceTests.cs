using AsistentePersonal.Data;
using AsistentePersonal.Models;
using AsistentePersonal.Services;
using Microsoft.EntityFrameworkCore;

namespace AsistentePersonal.Tests;

// The daily digest against a real PostgreSQL test database and a fake email sender (spec 005, RF-1).
// The owner's zone is America/La_Paz (UTC-4), so 12:00 UTC is 08:00 on Friday 2026-10-16.
public class ReminderServiceTests
{
    private const string OwnerEmail = "dueno@ejemplo.com";

    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");
    private static readonly DateTimeOffset At8 = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 16);

    // ---- CA-1.1, CA-1.5: the digest goes out from 8:00, to the owner's address ----

    [Fact]
    public async Task SendsAt8_SkipsAt759()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        await AddIdeaAsync(db, "Idea estancada", At8.AddDays(-20));
        var sender = new FakeEmailSender();
        var service = new ReminderService(db, sender);

        var early = await service.SendDailyAsync(At8.AddMinutes(-1), Zone);

        Assert.Equal(new ReminderOutcome(ReminderResult.Skipped, 0), early);
        Assert.Empty(sender.Messages);
        Assert.Equal(0, await db.DailyDigestLogs.CountAsync());

        var onTime = await service.SendDailyAsync(At8, Zone);

        Assert.Equal(new ReminderOutcome(ReminderResult.Sent, 1), onTime);
        Assert.Single(sender.Messages);
        Assert.Equal(Today, (await db.DailyDigestLogs.SingleAsync()).ForDate);
    }

    [Fact]
    public async Task SendsToOwnerEmail()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        var idea = await AddIdeaAsync(db, "Regalo para mamá", At8.AddDays(-28));
        var sender = new FakeEmailSender();

        await new ReminderService(db, sender).SendDailyAsync(At8, Zone);

        var message = Assert.Single(sender.Messages);
        Assert.Equal(OwnerEmail, message.To);
        Assert.Equal("Tu resumen de hoy: 1 idea", message.Subject);
        Assert.Equal("Ideas sin editar\n- \"Regalo para mamá\" (28 días)", message.Text);
        Assert.Equal("digest-2026-10-16", message.IdempotencyKey);
        var reminder = await db.ReminderLogs.SingleAsync();
        Assert.Equal((idea.Id, Today), (reminder.IdeaId, reminder.SentOn));
    }

    // ---- CA-1.2, CA-1.3: one digest per day, and none when there is nothing to report ----

    [Fact]
    public async Task SecondRunSameDay_Skips()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        await AddIdeaAsync(db, "Idea estancada", At8.AddDays(-20));
        var sender = new FakeEmailSender();
        var service = new ReminderService(db, sender);
        await service.SendDailyAsync(At8, Zone);

        var second = await service.SendDailyAsync(At8.AddHours(3), Zone);

        Assert.Equal(new ReminderOutcome(ReminderResult.Skipped, 0), second);
        Assert.Single(sender.Messages);
        Assert.Equal(1, await db.DailyDigestLogs.CountAsync());
    }

    [Fact]
    public async Task NothingToReport_SkipsWithoutEmailOrDigestRow()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        await AddIdeaAsync(db, "Idea reciente", At8.AddDays(-13));
        var sender = new FakeEmailSender();

        var outcome = await new ReminderService(db, sender).SendDailyAsync(At8, Zone);

        Assert.Equal(new ReminderOutcome(ReminderResult.Skipped, 0), outcome);
        Assert.Empty(sender.Messages);
        Assert.Equal(0, await db.DailyDigestLogs.CountAsync());
        Assert.Equal(0, await db.ReminderLogs.CountAsync());
    }

    [Fact]
    public async Task WithoutAnOwnerRow_Skips()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var sender = new FakeEmailSender();

        var outcome = await new ReminderService(db, sender).SendDailyAsync(At8, Zone);

        Assert.Equal(new ReminderOutcome(ReminderResult.Skipped, 0), outcome);
        Assert.Empty(sender.Messages);
    }

    // ---- CA-2.2, CA-2.5, CA-2.6: the memory of reminded ideas lives in the database ----

    [Fact]
    public async Task ArchivedIdea_IsNotIncluded()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        await AddIdeaAsync(db, "Idea archivada", At8.AddDays(-30), archivedAt: At8.AddDays(-20));
        await AddIdeaAsync(db, "Idea activa", At8.AddDays(-30));
        var sender = new FakeEmailSender();

        var outcome = await new ReminderService(db, sender).SendDailyAsync(At8, Zone);

        Assert.Equal(new ReminderOutcome(ReminderResult.Sent, 1), outcome);
        Assert.Equal("Ideas sin editar\n- \"Idea activa\" (30 días)", Assert.Single(sender.Messages).Text);
        Assert.Equal(1, await db.ReminderLogs.CountAsync());
    }

    [Fact]
    public async Task OnlyArchivedIdeas_SkipsTheDay()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        await AddIdeaAsync(db, "Idea archivada", At8.AddDays(-30), archivedAt: At8.AddDays(-20));
        var sender = new FakeEmailSender();

        var outcome = await new ReminderService(db, sender).SendDailyAsync(At8, Zone);

        Assert.Equal(ReminderResult.Skipped, outcome.Result);
        Assert.Empty(sender.Messages);
    }

    [Fact]
    public async Task RemindedIdea_ReturnsAfter14Days()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        await AddIdeaAsync(db, "Idea olvidada", At8.AddDays(-40));
        var sender = new FakeEmailSender();
        var service = new ReminderService(db, sender);

        var first = await service.SendDailyAsync(At8, Zone);
        var after13Days = await service.SendDailyAsync(At8.AddDays(13), Zone);
        var after14Days = await service.SendDailyAsync(At8.AddDays(14), Zone);

        Assert.Equal(ReminderResult.Sent, first.Result);
        Assert.Equal(ReminderResult.Skipped, after13Days.Result);
        Assert.Equal(ReminderResult.Sent, after14Days.Result);
        Assert.Equal(2, sender.Messages.Count);
        Assert.Equal(2, await db.ReminderLogs.CountAsync());
    }

    [Fact]
    public async Task Sending_KeepsUpdatedAtAndVersion()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        var idea = await AddIdeaAsync(db, "Idea intacta", At8.AddDays(-20));
        var versionBefore = idea.Version;

        await new ReminderService(db, new FakeEmailSender()).SendDailyAsync(At8, Zone);

        await using var check = await TestDatabase.OpenAsync();
        var saved = await check.Ideas.SingleAsync();
        Assert.Equal(1, await check.ReminderLogs.CountAsync());
        Assert.Equal(At8.AddDays(-20), saved.UpdatedAt);
        Assert.Equal(versionBefore, saved.Version);
    }

    [Fact]
    public async Task Ideas_AreListedOldestFirst()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        await AddIdeaAsync(db, "Veinte", At8.AddDays(-20));
        await AddIdeaAsync(db, "Cuarenta", At8.AddDays(-40));
        await AddIdeaAsync(db, "Treinta", At8.AddDays(-30));
        var sender = new FakeEmailSender();

        await new ReminderService(db, sender).SendDailyAsync(At8, Zone);

        Assert.Equal(
            [
                "Ideas sin editar",
                "- \"Cuarenta\" (40 días)",
                "- \"Treinta\" (30 días)",
                "- \"Veinte\" (20 días)",
            ],
            Assert.Single(sender.Messages).Text.Split('\n'));
    }

    // ---- CA-3.1 to CA-3.6: habit alerts come from the database ----

    [Fact]
    public async Task HabitAlert_ArrivesInDigest()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        await AddDailyHabitAsync(db, "Correr");
        var sender = new FakeEmailSender();

        var outcome = await new ReminderService(db, sender).SendDailyAsync(At8, Zone);

        Assert.Equal(new ReminderOutcome(ReminderResult.Sent, 1), outcome);
        var message = Assert.Single(sender.Messages);
        Assert.Equal("Tu resumen de hoy: 1 hábito", message.Subject);
        Assert.Equal("Hábitos en riesgo\n- Correr: ayer no lo cumpliste.", message.Text);
        Assert.Equal(1, await db.DailyDigestLogs.CountAsync());
    }

    [Fact]
    public async Task HabitLoggedYesterday_IsNotIncluded()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        var habit = await AddDailyHabitAsync(db, "Correr");
        db.HabitLogs.Add(new HabitLog { HabitId = habit.Id, Date = Today.AddDays(-1) });
        await db.SaveChangesAsync();
        var sender = new FakeEmailSender();

        var outcome = await new ReminderService(db, sender).SendDailyAsync(At8, Zone);

        Assert.Equal(ReminderResult.Skipped, outcome.Result);
        Assert.Empty(sender.Messages);
    }

    [Fact]
    public async Task DeletedHabit_IsNotIncluded()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        var habit = await AddDailyHabitAsync(db, "Correr");
        db.Habits.Remove(habit);
        await db.SaveChangesAsync();
        var sender = new FakeEmailSender();

        var outcome = await new ReminderService(db, sender).SendDailyAsync(At8, Zone);

        Assert.Equal(ReminderResult.Skipped, outcome.Result);
        Assert.Empty(sender.Messages);
    }

    [Fact]
    public async Task Items_CountIdeasPlusDistinctHabits()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        await AddIdeaAsync(db, "Idea estancada", At8.AddDays(-20));
        var habit = await AddDailyHabitAsync(db, "Correr");
        foreach (var day in Enumerable.Range(2, 5))
        {
            db.HabitLogs.Add(new HabitLog { HabitId = habit.Id, Date = Today.AddDays(-day) });
        }

        await db.SaveChangesAsync();
        var sender = new FakeEmailSender();

        var outcome = await new ReminderService(db, sender).SendDailyAsync(At8, Zone);

        // The habit has two alerts (missed yesterday, lost a streak of 5) but counts once.
        Assert.Equal(new ReminderOutcome(ReminderResult.Sent, 2), outcome);
        var message = Assert.Single(sender.Messages);
        Assert.Equal("Tu resumen de hoy: 1 idea y 1 hábito", message.Subject);
        Assert.Contains("- Correr: perdiste una racha de 5 días.", message.Text);
    }

    [Fact]
    public async Task OnlyLast14DaysOfLogs_GiveSameAlertsAsFullHistory()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        var habit = await AddDailyHabitAsync(db, "Correr");
        var oldDays = Enumerable.Range(20, 60).Select(day => Today.AddDays(-day));
        var recentDays = Enumerable.Range(2, 5).Select(day => Today.AddDays(-day));
        var allDays = oldDays.Concat(recentDays).ToList();
        db.HabitLogs.AddRange(allDays.Select(date => new HabitLog { HabitId = habit.Id, Date = date }));
        await db.SaveChangesAsync();
        var sender = new FakeEmailSender();

        await new ReminderService(db, sender).SendDailyAsync(At8, Zone);

        var fullHistory = ReminderRules.HabitAlerts(habit, allDays.ToHashSet(), Today);
        Assert.Equal(2, fullHistory.Count);
        Assert.Equal(ReminderText.Body([], fullHistory), Assert.Single(sender.Messages).Text);
    }

    // ---- CA-1.4: a failed send leaves no trace, so the next run of the day tries again ----

    [Fact]
    public async Task SendFails_LeavesNoRows_AndNextRunSends()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        await AddIdeaAsync(db, "Idea estancada", At8.AddDays(-20));
        var sender = new FakeEmailSender { Accepts = false };
        var service = new ReminderService(db, sender);

        var failed = await service.SendDailyAsync(At8, Zone);

        Assert.Equal(new ReminderOutcome(ReminderResult.Failed, 1), failed);
        Assert.Single(sender.Messages);
        Assert.Equal(0, await db.DailyDigestLogs.CountAsync());
        Assert.Equal(0, await db.ReminderLogs.CountAsync());

        sender.Accepts = true;
        var retry = await service.SendDailyAsync(At8.AddHours(1), Zone);

        Assert.Equal(new ReminderOutcome(ReminderResult.Sent, 1), retry);
        Assert.Equal(1, await db.DailyDigestLogs.CountAsync());
        Assert.Equal(1, await db.ReminderLogs.CountAsync());
    }

    // ---- CA-1.3, RNF-3: two runs at the same time send one email ----

    [Fact]
    public async Task ConcurrentRun_UniqueViolation_Skips()
    {
        await using (var seed = await TestDatabase.CreateEmptyContextAsync())
        {
            await AddOwnerAsync(seed);
            await AddIdeaAsync(seed, "Idea estancada", At8.AddDays(-20));
        }

        // Another run saves its row for the same day right before ours does.
        await using var db = await TestDatabase.OpenAsync(new BeforeSaveInterceptor(async () =>
        {
            await using var otherRun = await TestDatabase.OpenAsync();
            otherRun.DailyDigestLogs.Add(new DailyDigestLog { ForDate = Today, SentAt = At8 });
            await otherRun.SaveChangesAsync();
        }));
        var sender = new FakeEmailSender();

        var outcome = await new ReminderService(db, sender).SendDailyAsync(At8, Zone);

        Assert.Equal(new ReminderOutcome(ReminderResult.Skipped, 0), outcome);
        Assert.Empty(sender.Messages);
        await using var check = await TestDatabase.OpenAsync();
        Assert.Equal(1, await check.DailyDigestLogs.CountAsync());
        Assert.Equal(0, await check.ReminderLogs.CountAsync());
    }

    // ---- Helpers ----

    private static async Task AddOwnerAsync(AppDbContext db)
    {
        db.Users.Add(new User { Id = User.OwnerId, Email = OwnerEmail, PasswordHash = "hash-de-prueba", CreatedAt = At8.AddDays(-90) });
        await db.SaveChangesAsync();
    }

    private static async Task<Habit> AddDailyHabitAsync(AppDbContext db, string name)
    {
        var habit = new Habit
        {
            UserId = User.OwnerId, Name = name, Frequency = Frequency.Daily, CreatedOn = Today.AddDays(-60),
        };
        db.Habits.Add(habit);
        await db.SaveChangesAsync();
        return habit;
    }

    private static async Task<Idea> AddIdeaAsync(
        AppDbContext db, string text, DateTimeOffset updatedAt, DateTimeOffset? archivedAt = null)
    {
        var idea = new Idea
        {
            UserId = User.OwnerId, Text = text, CreatedAt = updatedAt, UpdatedAt = updatedAt, ArchivedAt = archivedAt,
        };
        db.Ideas.Add(idea);
        await db.SaveChangesAsync();
        return idea;
    }
}
