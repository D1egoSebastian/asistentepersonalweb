using AsistentePersonal.Data;
using AsistentePersonal.Models;
using AsistentePersonal.Services;
using Microsoft.EntityFrameworkCore;

namespace AsistentePersonal.Tests;

// The restrictions that keep the digest honest (spec 005, RNF-3, CA-2.2): one digest per day,
// one reminder per idea per day, and no reminders left behind when an idea is deleted.
public class ReminderModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Day = new(2026, 10, 16);

    [Fact]
    public async Task DailyDigestLog_SecondRowForTheSameDate_IsRejected()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        db.DailyDigestLogs.Add(new DailyDigestLog { ForDate = Day, SentAt = Now });
        await db.SaveChangesAsync();

        await using var other = await TestDatabase.OpenAsync();
        other.DailyDigestLogs.Add(new DailyDigestLog { ForDate = Day, SentAt = Now.AddMinutes(1) });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => other.SaveChangesAsync());

        Assert.True(DatabaseErrors.IsUniqueViolation(error));
    }

    [Fact]
    public async Task DailyDigestLog_DifferentDates_AreBothSaved()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        db.DailyDigestLogs.Add(new DailyDigestLog { ForDate = Day, SentAt = Now });
        db.DailyDigestLogs.Add(new DailyDigestLog { ForDate = Day.AddDays(1), SentAt = Now.AddDays(1) });
        await db.SaveChangesAsync();

        Assert.Equal(2, await db.DailyDigestLogs.CountAsync());
    }

    [Fact]
    public async Task ReminderLog_SecondRowForTheSameIdeaAndDay_IsRejected()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var ideaId = await AddIdeaAsync(db);
        db.ReminderLogs.Add(new ReminderLog { IdeaId = ideaId, SentOn = Day });
        await db.SaveChangesAsync();

        await using var other = await TestDatabase.OpenAsync();
        other.ReminderLogs.Add(new ReminderLog { IdeaId = ideaId, SentOn = Day });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => other.SaveChangesAsync());

        Assert.True(DatabaseErrors.IsUniqueViolation(error));
    }

    [Fact]
    public async Task ReminderLog_SameIdeaOnAnotherDay_IsSaved()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var ideaId = await AddIdeaAsync(db);
        db.ReminderLogs.Add(new ReminderLog { IdeaId = ideaId, SentOn = Day });
        db.ReminderLogs.Add(new ReminderLog { IdeaId = ideaId, SentOn = Day.AddDays(14) });
        await db.SaveChangesAsync();

        Assert.Equal(2, await db.ReminderLogs.CountAsync());
    }

    [Fact]
    public async Task DeletingAnIdea_DeletesItsReminderLogs()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var ideaId = await AddIdeaAsync(db);
        db.ReminderLogs.Add(new ReminderLog { IdeaId = ideaId, SentOn = Day });
        await db.SaveChangesAsync();
        Assert.Equal(1, await db.ReminderLogs.CountAsync());

        await db.Ideas.ExecuteDeleteAsync();

        Assert.Equal(0, await db.ReminderLogs.CountAsync());
    }

    private static async Task<int> AddIdeaAsync(AppDbContext db)
    {
        db.Users.Add(new User { Id = User.OwnerId, Email = "dueno@ejemplo.com", PasswordHash = "hash-de-prueba", CreatedAt = Now });
        var idea = new Idea { UserId = User.OwnerId, Text = "Una idea", CreatedAt = Now, UpdatedAt = Now };
        db.Ideas.Add(idea);
        await db.SaveChangesAsync();
        return idea.Id;
    }
}
