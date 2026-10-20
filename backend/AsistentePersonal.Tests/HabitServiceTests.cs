using AsistentePersonal.Models;
using AsistentePersonal.Services;
using Microsoft.EntityFrameworkCore;

namespace AsistentePersonal.Tests;

public class HabitServiceTests
{
    private const int UserId = 1;
    private const int OtherUserId = 2;

    // Fixed "today" so the tests never depend on the real clock.
    private static readonly DateOnly Today = new(2026, 10, 16);

    // ---- T17: Create ----

    [Fact]
    public async Task Create_NormalizesName_AndStoresCreatedOnAsToday()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);

        var result = await service.CreateAsync(UserId, "  Leer  ", null, Frequency.Daily, null, Today);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("Leer", result.Value!.Name);
        Assert.Equal(Today, result.Value.CreatedOn);
    }

    [Fact]
    public async Task Create_DailyHabit_IgnoresGoal()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);

        var result = await service.CreateAsync(UserId, "Leer", null, Frequency.Daily, 5, Today);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Null(result.Value!.WeeklyGoal);
    }

    [Fact]
    public async Task Create_WeeklyHabit_StoresGoal()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);

        var result = await service.CreateAsync(UserId, "Gimnasio", null, Frequency.Weekly, 3, Today);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(3, result.Value!.WeeklyGoal);
    }

    [Fact]
    public async Task Create_RejectsBlankName()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);

        var result = await service.CreateAsync(UserId, "   ", null, Frequency.Daily, null, Today);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal(0, await db.Habits.CountAsync());
    }

    [Fact]
    public async Task Create_RejectsWeeklyHabitWithoutGoal()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);

        var result = await service.CreateAsync(UserId, "Gimnasio", null, Frequency.Weekly, null, Today);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Create_RejectsNameUsedByAnotherHabit_IgnoringCase()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        await service.CreateAsync(UserId, "Leer", null, Frequency.Daily, null, Today);

        var result = await service.CreateAsync(UserId, "  leer  ", null, Frequency.Daily, null, Today);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal(1, await db.Habits.CountAsync());
    }

    [Fact]
    public async Task Create_AllowsSameName_ForAnotherOwner()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        await service.CreateAsync(UserId, "Leer", null, Frequency.Daily, null, Today);

        var result = await service.CreateAsync(OtherUserId, "Leer", null, Frequency.Daily, null, Today);

        Assert.Equal(ResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Create_RejectsThe51stHabit()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        for (var i = 1; i <= 50; i++)
        {
            await service.CreateAsync(UserId, $"Hábito {i}", null, Frequency.Daily, null, Today);
        }

        var result = await service.CreateAsync(UserId, "Hábito 51", null, Frequency.Daily, null, Today);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    // ---- T18: List ----

    [Fact]
    public async Task List_ReturnsToday_AndHabitsOrderedByCreatedOnThenId_WithTheirLogDates()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        var createdLater = await CreateDailyAsync(service, "A", new DateOnly(2026, 10, 10));
        await CreateDailyAsync(service, "B", new DateOnly(2026, 10, 1));
        await service.LogAsync(UserId, createdLater, Today, Today);

        var list = await service.ListAsync(UserId, Today);

        Assert.Equal(Today, list.Today);
        Assert.Equal(new[] { "B", "A" }, list.Habits.Select(h => h.Habit.Name).ToArray());
        Assert.Empty(list.Habits[0].LogDates);
        Assert.Equal(new[] { Today }, list.Habits[1].LogDates);
    }

    [Fact]
    public async Task List_ReturnsOnlyTheOwnersHabits()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        await CreateDailyAsync(service, "Mío", Today);
        await service.CreateAsync(OtherUserId, "Ajeno", null, Frequency.Daily, null, Today);

        var list = await service.ListAsync(UserId, Today);

        Assert.Equal(new[] { "Mío" }, list.Habits.Select(h => h.Habit.Name).ToArray());
    }

    // ---- T19: Update ----

    [Fact]
    public async Task Update_ChangesNameAndDescription_AndKeepsLogsCreatedOnAndGoal()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        var created = await service.CreateAsync(UserId, "Gimnasio", null, Frequency.Weekly, 3, Today);
        var habitId = created.Value!.Id;
        await service.LogAsync(UserId, habitId, Today, Today);

        var result = await service.UpdateAsync(UserId, habitId, "Ir al gimnasio", "Lunes, miércoles y viernes");

        Assert.Equal(ResultStatus.Ok, result.Status);
        var saved = await db.Habits.SingleAsync(h => h.Id == habitId);
        Assert.Equal("Ir al gimnasio", saved.Name);
        Assert.Equal("Lunes, miércoles y viernes", saved.Description);
        Assert.Equal(3, saved.WeeklyGoal);
        Assert.Equal(Today, saved.CreatedOn);
        Assert.Equal(1, await db.HabitLogs.CountAsync());
    }

    [Fact]
    public async Task Update_WithoutChanges_ReturnsOk()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        var habitId = await CreateDailyAsync(service, "Leer", Today);

        var result = await service.UpdateAsync(UserId, habitId, "Leer", null);

        Assert.Equal(ResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Update_BlankDescription_IsStoredAsNull()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        var habitId = await CreateDailyAsync(service, "Leer", Today);
        await service.UpdateAsync(UserId, habitId, "Leer", "Algo");

        await service.UpdateAsync(UserId, habitId, "Leer", "   ");

        Assert.Null((await db.Habits.SingleAsync(h => h.Id == habitId)).Description);
    }

    [Fact]
    public async Task Update_RejectsBlankName_WithoutChangingAnything()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        var habitId = await CreateDailyAsync(service, "Leer", Today);

        var result = await service.UpdateAsync(UserId, habitId, "  ", "Nueva");

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal("Leer", (await db.Habits.SingleAsync(h => h.Id == habitId)).Name);
    }

    [Fact]
    public async Task Update_RejectsNameUsedByAnotherHabit_IgnoringCase()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        await CreateDailyAsync(service, "Leer", Today);
        var otherId = await CreateDailyAsync(service, "Escribir", Today);

        var result = await service.UpdateAsync(UserId, otherId, "LEER", null);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Update_UnknownHabit_ReturnsNotFound()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);

        var result = await service.UpdateAsync(UserId, 999, "Leer", null);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    // ---- T20: Delete ----

    [Fact]
    public async Task Delete_RemovesHabitAndItsLogs()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        var habitId = await CreateDailyAsync(service, "Leer", Today);
        await service.LogAsync(UserId, habitId, Today, Today);

        var result = await service.DeleteAsync(UserId, habitId);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(0, await db.Habits.CountAsync());
        Assert.Equal(0, await db.HabitLogs.CountAsync());
    }

    [Fact]
    public async Task Delete_UnknownHabit_ReturnsNotFound()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);

        var result = await service.DeleteAsync(UserId, 999);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    // ---- T21: Log ----

    [Fact]
    public async Task Log_Today_StoresRecord()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        var habitId = await CreateDailyAsync(service, "Leer", Today.AddDays(-5));

        var result = await service.LogAsync(UserId, habitId, Today, Today);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(new[] { Today }, await db.HabitLogs.Select(l => l.Date).ToArrayAsync());
    }

    [Fact]
    public async Task Log_Yesterday_IsAccepted()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        var habitId = await CreateDailyAsync(service, "Leer", Today.AddDays(-5));

        var result = await service.LogAsync(UserId, habitId, Today.AddDays(-1), Today);

        Assert.Equal(ResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Log_TwiceOnTheSameDay_KeepsOneRecord_AndReturnsOk()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        var habitId = await CreateDailyAsync(service, "Leer", Today.AddDays(-5));

        await service.LogAsync(UserId, habitId, Today, Today);
        var second = await service.LogAsync(UserId, habitId, Today, Today);

        Assert.Equal(ResultStatus.Ok, second.Status);
        Assert.Equal(1, await db.HabitLogs.CountAsync());
    }

    [Fact]
    public async Task Log_DayBeforeYesterday_IsInvalid()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        var habitId = await CreateDailyAsync(service, "Leer", Today.AddDays(-5));

        var result = await service.LogAsync(UserId, habitId, Today.AddDays(-2), Today);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal(0, await db.HabitLogs.CountAsync());
    }

    [Fact]
    public async Task Log_FutureDate_IsInvalid()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        var habitId = await CreateDailyAsync(service, "Leer", Today.AddDays(-5));

        var result = await service.LogAsync(UserId, habitId, Today.AddDays(1), Today);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Log_DateBeforeHabitCreation_IsInvalid()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        var habitId = await CreateDailyAsync(service, "Leer", Today);

        var result = await service.LogAsync(UserId, habitId, Today.AddDays(-1), Today);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Log_UnknownHabit_ReturnsNotFound()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);

        var result = await service.LogAsync(UserId, 999, Today, Today);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    // ---- T22: Log concurrent ----

    [Fact]
    public async Task Log_ConcurrentSameDay_LeavesExactlyOneRecord_WithoutErrors()
    {
        await using var first = await TestDatabase.CreateEmptyContextAsync();
        var habitId = await CreateDailyAsync(new HabitService(first), "Leer", Today.AddDays(-5));
        await using var second = await TestDatabase.OpenAsync();

        var results = await Task.WhenAll(
            new HabitService(first).LogAsync(UserId, habitId, Today, Today),
            new HabitService(second).LogAsync(UserId, habitId, Today, Today));

        Assert.All(results, r => Assert.Equal(ResultStatus.Ok, r.Status));
        await using var check = await TestDatabase.OpenAsync();
        Assert.Equal(1, await check.HabitLogs.CountAsync());
    }

    // ---- T23: Unlog ----

    [Fact]
    public async Task Unlog_RemovesRecord()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        var habitId = await CreateDailyAsync(service, "Leer", Today.AddDays(-5));
        await service.LogAsync(UserId, habitId, Today, Today);

        var result = await service.UnlogAsync(UserId, habitId, Today, Today);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(0, await db.HabitLogs.CountAsync());
    }

    [Fact]
    public async Task Unlog_DayWithoutRecord_ReturnsOk()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        var habitId = await CreateDailyAsync(service, "Leer", Today.AddDays(-5));

        var result = await service.UnlogAsync(UserId, habitId, Today, Today);

        Assert.Equal(ResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Unlog_DayBeforeYesterday_IsInvalid()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        var habitId = await CreateDailyAsync(service, "Leer", Today.AddDays(-5));

        var result = await service.UnlogAsync(UserId, habitId, Today.AddDays(-2), Today);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Unlog_UnknownHabit_ReturnsNotFound()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);

        var result = await service.UnlogAsync(UserId, 999, Today, Today);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    // ---- Races: another request acts between our checks and our save (CA-10.1, CA-1.8) ----

    [Fact]
    public async Task Update_WhenTheHabitIsDeletedMeanwhile_ReturnsNotFound()
    {
        await using var setup = await TestDatabase.CreateEmptyContextAsync();
        var habitId = await CreateDailyAsync(new HabitService(setup), "Leer", Today);
        await using var db = await TestDatabase.OpenAsync(new BeforeSaveInterceptor(() => DeleteElsewhereAsync(habitId)));

        var result = await new HabitService(db).UpdateAsync(UserId, habitId, "Leer libros", null);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Delete_WhenTheHabitIsDeletedMeanwhile_ReturnsNotFound()
    {
        await using var setup = await TestDatabase.CreateEmptyContextAsync();
        var habitId = await CreateDailyAsync(new HabitService(setup), "Leer", Today);
        await using var db = await TestDatabase.OpenAsync(new BeforeSaveInterceptor(() => DeleteElsewhereAsync(habitId)));

        var result = await new HabitService(db).DeleteAsync(UserId, habitId);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Log_WhenTheHabitIsDeletedMeanwhile_ReturnsNotFound()
    {
        await using var setup = await TestDatabase.CreateEmptyContextAsync();
        var habitId = await CreateDailyAsync(new HabitService(setup), "Leer", Today.AddDays(-5));
        await using var db = await TestDatabase.OpenAsync(new BeforeSaveInterceptor(() => DeleteElsewhereAsync(habitId)));

        var result = await new HabitService(db).LogAsync(UserId, habitId, Today, Today);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Create_WhenTheSameNameIsTakenMeanwhile_ReturnsInvalid()
    {
        await using var setup = await TestDatabase.CreateEmptyContextAsync();
        await using var db = await TestDatabase.OpenAsync(new BeforeSaveInterceptor(async () =>
        {
            await using var other = await TestDatabase.OpenAsync();
            await new HabitService(other).CreateAsync(UserId, "Leer", null, Frequency.Daily, null, Today);
        }));

        var result = await new HabitService(db).CreateAsync(UserId, "LEER", null, Frequency.Daily, null, Today);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal(1, await setup.Habits.CountAsync());
    }

    [Fact]
    public async Task Create_RejectsNameWithAccentsAndEnye_IgnoringCase()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        await service.CreateAsync(UserId, "Ñandú", null, Frequency.Daily, null, Today);

        var result = await service.CreateAsync(UserId, "ñandú", null, Frequency.Daily, null, Today);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    // ---- Ownership: an owner cannot change another owner's habit (RNF-1) ----

    [Fact]
    public async Task AnotherOwner_CannotUpdateDeleteLogOrUnlogTheHabit()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        var service = new HabitService(db);
        var habitId = await CreateDailyAsync(service, "Leer", Today.AddDays(-5));
        await service.LogAsync(UserId, habitId, Today, Today);

        Assert.Equal(ResultStatus.NotFound, (await service.UpdateAsync(OtherUserId, habitId, "Otro", null)).Status);
        Assert.Equal(ResultStatus.NotFound, (await service.LogAsync(OtherUserId, habitId, Today.AddDays(-1), Today)).Status);
        Assert.Equal(ResultStatus.NotFound, (await service.UnlogAsync(OtherUserId, habitId, Today, Today)).Status);
        Assert.Equal(ResultStatus.NotFound, (await service.DeleteAsync(OtherUserId, habitId)).Status);
        Assert.Equal("Leer", (await db.Habits.SingleAsync()).Name);
        Assert.Equal(1, await db.HabitLogs.CountAsync());
    }

    // Deletes the habit from a separate connection, as another request would.
    private static async Task DeleteElsewhereAsync(int habitId)
    {
        await using var other = await TestDatabase.OpenAsync();
        await other.Habits.Where(h => h.Id == habitId).ExecuteDeleteAsync();
    }

    // Creates a daily habit for the test owner and returns its id.
    private static async Task<int> CreateDailyAsync(HabitService service, string name, DateOnly createdOn)
    {
        var result = await service.CreateAsync(UserId, name, null, Frequency.Daily, null, createdOn);
        Assert.Equal(ResultStatus.Ok, result.Status);
        return result.Value!.Id;
    }
}
