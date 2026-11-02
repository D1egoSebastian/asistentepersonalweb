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

    // ---- CA-1.4, RNF-3: a run that rolls back frees the day for the run that was waiting ----

    [Fact]
    public async Task ConcurrentRun_FirstSendFails_SecondRunWaitsThenSends()
    {
        await using (var seed = await TestDatabase.CreateEmptyContextAsync())
        {
            await AddOwnerAsync(seed);
            await AddIdeaAsync(seed, "Idea estancada", At8.AddDays(-20));
        }

        await using var first = await TestDatabase.OpenAsync();
        await using var second = await TestDatabase.OpenAsync();
        var secondSender = new FakeEmailSender();
        Task<ReminderOutcome>? secondRun = null;
        var secondWasWaiting = false;

        // While the first run holds the day's row inside its open transaction, the second run starts and has to
        // wait for it. Then the first send fails, its transaction rolls back and the row disappears.
        var firstSender = new CallbackEmailSender(async () =>
        {
            secondRun = new ReminderService(second, secondSender).SendDailyAsync(At8, Zone);
            await WaitUntilALockIsWaitingAsync();
            secondWasWaiting = !secondRun.IsCompleted;
            return false;
        });

        var firstOutcome = await new ReminderService(first, firstSender).SendDailyAsync(At8, Zone);
        var secondOutcome = await secondRun!.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.True(secondWasWaiting);
        Assert.Equal(new ReminderOutcome(ReminderResult.Failed, 1), firstOutcome);
        Assert.Equal(new ReminderOutcome(ReminderResult.Sent, 1), secondOutcome);
        Assert.Single(secondSender.Messages);
        await using var check = await TestDatabase.OpenAsync();
        Assert.Equal(1, await check.DailyDigestLogs.CountAsync());
        Assert.Equal(1, await check.ReminderLogs.CountAsync());
    }

    // An idea deleted between loading it and saving its reminder breaks the foreign key. The run ends as
    // "failed" (502) with nothing stored, and the next run goes on without that idea.
    [Fact]
    public async Task IdeaDeletedBeforeSave_FailsWithoutRows_AndNextRunSends()
    {
        int ideaId;
        await using (var seed = await TestDatabase.CreateEmptyContextAsync())
        {
            await AddOwnerAsync(seed);
            ideaId = (await AddIdeaAsync(seed, "Idea que se borra", At8.AddDays(-20))).Id;
            await AddIdeaAsync(seed, "Idea que queda", At8.AddDays(-30));
        }

        await using var db = await TestDatabase.OpenAsync(new BeforeSaveInterceptor(async () =>
        {
            await using var otherRequest = await TestDatabase.OpenAsync();
            await otherRequest.Ideas.Where(i => i.Id == ideaId).ExecuteDeleteAsync();
        }));
        var sender = new FakeEmailSender();
        var service = new ReminderService(db, sender);

        var failed = await service.SendDailyAsync(At8, Zone);

        Assert.Equal(ReminderResult.Failed, failed.Result);
        Assert.Empty(sender.Messages);
        await using (var check = await TestDatabase.OpenAsync())
        {
            Assert.Equal(0, await check.DailyDigestLogs.CountAsync());
            Assert.Equal(0, await check.ReminderLogs.CountAsync());
        }

        var retry = await service.SendDailyAsync(At8.AddHours(1), Zone);

        Assert.Equal(new ReminderOutcome(ReminderResult.Sent, 1), retry);
        Assert.Equal("Ideas sin editar\n- \"Idea que queda\" (30 días)", Assert.Single(sender.Messages).Text);
    }

    // ---- D-1, RNF-4: "today" is the owner's calendar day, not the UTC date ----
    // In La Paz (UTC-4) the digest window is 12:00 UTC to 03:59 UTC of the next day. 02:00 UTC on 2026-10-17
    // is 22:00 on Friday 2026-10-16: the UTC date is already Saturday, the owner's day is still Friday.

    private static readonly DateTimeOffset FridayEvening = new(2026, 10, 17, 2, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task LocalEvening_StillCountsAsTheOwnersDay()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        await AddIdeaAsync(db, "Idea estancada", At8.AddDays(-20));
        var sender = new FakeEmailSender();

        var outcome = await new ReminderService(db, sender).SendDailyAsync(FridayEvening, Zone);

        Assert.Equal(new ReminderOutcome(ReminderResult.Sent, 1), outcome);
        var message = Assert.Single(sender.Messages);
        Assert.Equal("digest-2026-10-16", message.IdempotencyKey);
        Assert.Equal("Ideas sin editar\n- \"Idea estancada\" (20 días)", message.Text);
        Assert.Equal(Today, (await db.DailyDigestLogs.SingleAsync()).ForDate);
        Assert.Equal(Today, (await db.ReminderLogs.SingleAsync()).SentOn);
    }

    [Fact]
    public async Task LocalEvening_AfterTheMorningDigest_Skips()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        await AddDailyHabitAsync(db, "Correr");
        var sender = new FakeEmailSender();
        var service = new ReminderService(db, sender);
        await service.SendDailyAsync(At8, Zone);

        // The UTC date changed, the owner's day did not: the habit alert is still there, but it was already sent.
        var evening = await service.SendDailyAsync(FridayEvening, Zone);

        Assert.Equal(new ReminderOutcome(ReminderResult.Skipped, 0), evening);
        Assert.Single(sender.Messages);
        Assert.Equal(1, await db.DailyDigestLogs.CountAsync());
    }

    [Theory]
    [InlineData(3, 59, ReminderResult.Sent)] // 23:59 on the owner's Friday
    [InlineData(4, 0, ReminderResult.Skipped)] // 00:00 on the owner's Saturday: before 8:00
    public async Task LocalMidnight_ClosesTheDigestWindow(int utcHour, int utcMinute, ReminderResult expected)
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        await AddIdeaAsync(db, "Idea estancada", At8.AddDays(-20));
        var utcNow = new DateTimeOffset(2026, 10, 17, utcHour, utcMinute, 0, TimeSpan.Zero);

        var outcome = await new ReminderService(db, new FakeEmailSender()).SendDailyAsync(utcNow, Zone);

        Assert.Equal(expected, outcome.Result);
    }

    [Theory]
    [InlineData(2, ReminderResult.Sent, "(14 días)")] // 22:00 on 2026-10-02 locally: 14 days before Friday 16th
    [InlineData(4, ReminderResult.Skipped, "")] // 00:00 on 2026-10-03 locally: only 13 days
    public async Task IdeaEditedAroundUtcMidnight_CountsTheOwnersDay(int editedAtUtcHour, ReminderResult expected, string days)
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        await AddIdeaAsync(db, "Idea de la noche", new DateTimeOffset(2026, 10, 3, editedAtUtcHour, 0, 0, TimeSpan.Zero));
        var sender = new FakeEmailSender();

        var outcome = await new ReminderService(db, sender).SendDailyAsync(At8, Zone);

        Assert.Equal(expected, outcome.Result);
        if (expected == ReminderResult.Sent)
        {
            Assert.Equal($"Ideas sin editar\n- \"Idea de la noche\" {days}", Assert.Single(sender.Messages).Text);
        }
    }

    // ---- CA-2.2, CA-2.5: the second reminder counts from the last edit, not from the last reminder ----

    [Fact]
    public async Task RemindedTwice_DaysCountFromTheLastEdit()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        await AddIdeaAsync(db, "Idea olvidada", At8.AddDays(-40));
        var sender = new FakeEmailSender();
        var service = new ReminderService(db, sender);

        await service.SendDailyAsync(At8, Zone);
        await service.SendDailyAsync(At8.AddDays(14), Zone);

        Assert.Equal(
            ["Ideas sin editar\n- \"Idea olvidada\" (40 días)", "Ideas sin editar\n- \"Idea olvidada\" (54 días)"],
            sender.Messages.Select(m => m.Text));
    }

    [Fact]
    public async Task IdeaRestoredToday_IsNotIncluded_UntilItIsStaleAgain()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerAsync(db);
        var idea = await AddIdeaAsync(db, "Idea restaurada", At8.AddDays(-30), archivedAt: At8.AddDays(-20));
        await new IdeaService(db).RestoreAsync(User.OwnerId, idea.Id, At8);
        var sender = new FakeEmailSender();
        var service = new ReminderService(db, sender);

        var today = await service.SendDailyAsync(At8, Zone);
        var after13Days = await service.SendDailyAsync(At8.AddDays(13), Zone);
        var after14Days = await service.SendDailyAsync(At8.AddDays(14), Zone);

        Assert.Equal(ReminderResult.Skipped, today.Result);
        Assert.Equal(ReminderResult.Skipped, after13Days.Result);
        Assert.Equal(ReminderResult.Sent, after14Days.Result);
        Assert.Equal("Ideas sin editar\n- \"Idea restaurada\" (14 días)", Assert.Single(sender.Messages).Text);
    }

    // ---- Helpers ----

    // An email sender whose answer comes from a test action, so a test can act while a run is sending.
    private sealed class CallbackEmailSender(Func<Task<bool>> onSend) : IEmailSender
    {
        public Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken) => onSend();
    }

    // True evidence that another run is stuck behind a row lock, instead of guessing with a sleep.
    private static async Task WaitUntilALockIsWaitingAsync()
    {
        await using var probe = await TestDatabase.OpenAsync();
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var waiting = await probe.Database
                .SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM pg_locks WHERE NOT granted")
                .SingleAsync();
            if (waiting > 0)
            {
                return;
            }

            await Task.Delay(25);
        }

        throw new TimeoutException("The second run never waited for the first one.");
    }

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
