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

    // ---- Helpers ----

    private static async Task AddOwnerAsync(AppDbContext db)
    {
        db.Users.Add(new User { Id = User.OwnerId, Email = OwnerEmail, PasswordHash = "hash-de-prueba", CreatedAt = At8.AddDays(-90) });
        await db.SaveChangesAsync();
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
