using AsistentePersonal.Data;
using AsistentePersonal.Models;
using Microsoft.EntityFrameworkCore;

namespace AsistentePersonal.Services;

// Use cases for habits. Every method takes the owner (userId) and, when it needs dates, "today".
// The service never reads the clock: the caller decides what "today" is.
public class HabitService(AppDbContext db)
{
    public const string NotFoundMessage = "Este hábito ya no existe.";

    private const string NameTakenMessage = "Ya existe un hábito con ese nombre.";

    public async Task<ServiceResult<Habit>> CreateAsync(
        int userId, string? name, string? description, Frequency frequency, int? weeklyGoal, DateOnly today)
    {
        var error = await ValidateNewHabitAsync(userId, name, description, frequency, weeklyGoal);
        if (error is not null)
        {
            return ServiceResult<Habit>.Invalid(error);
        }

        var cleanName = HabitRules.Normalize(name)!;
        var habit = new Habit
        {
            UserId = userId,
            Name = cleanName,
            Description = HabitRules.Normalize(description),
            Frequency = frequency,
            WeeklyGoal = frequency == Frequency.Weekly ? weeklyGoal : null,
            CreatedOn = today,
        };
        db.Habits.Add(habit);
        var saved = await SaveHabitChangesAsync();
        return saved.Status == ResultStatus.Ok
            ? ServiceResult<Habit>.Ok(habit)
            : ServiceResult<Habit>.Invalid(saved.Message!);
    }

    public async Task<HabitList> ListAsync(int userId, DateOnly today)
    {
        var habits = await db.Habits
            .Where(h => h.UserId == userId)
            .OrderBy(h => h.CreatedOn)
            .ThenBy(h => h.Id)
            .ToListAsync();

        var habitIds = habits.Select(h => h.Id).ToList();
        var logs = await db.HabitLogs
            .Where(l => habitIds.Contains(l.HabitId))
            .ToListAsync();

        var items = habits
            .Select(h => new HabitWithLogs(
                h,
                logs.Where(l => l.HabitId == h.Id).Select(l => l.Date).OrderBy(d => d).ToList()))
            .ToList();

        return new HabitList(today, items);
    }

    public async Task<ServiceResult> UpdateAsync(int userId, int habitId, string? name, string? description)
    {
        var habit = await FindOwnedAsync(userId, habitId);
        if (habit is null)
        {
            return ServiceResult.NotFound();
        }

        var error = HabitRules.ValidateName(name) ?? HabitRules.ValidateDescription(description);
        if (error is not null)
        {
            return ServiceResult.Invalid(error);
        }

        var cleanName = HabitRules.Normalize(name)!;
        if (await NameIsTakenAsync(userId, cleanName, exceptHabitId: habitId))
        {
            return ServiceResult.Invalid(NameTakenMessage);
        }

        habit.Name = cleanName;
        habit.Description = HabitRules.Normalize(description);
        return await SaveHabitChangesAsync();
    }

    public async Task<ServiceResult> DeleteAsync(int userId, int habitId)
    {
        var habit = await FindOwnedAsync(userId, habitId);
        if (habit is null)
        {
            return ServiceResult.NotFound();
        }

        // The database deletes the habit's logs too (ON DELETE CASCADE).
        db.Habits.Remove(habit);
        return await SaveHabitChangesAsync();
    }

    public async Task<ServiceResult> LogAsync(int userId, int habitId, DateOnly date, DateOnly today)
    {
        var habit = await FindOwnedAsync(userId, habitId);
        if (habit is null)
        {
            return ServiceResult.NotFound();
        }

        var error = HabitRules.ValidateLogDate(date, today, habit.CreatedOn);
        if (error is not null)
        {
            return ServiceResult.Invalid(error);
        }

        if (await db.HabitLogs.AnyAsync(l => l.HabitId == habitId && l.Date == date))
        {
            return ServiceResult.Ok();
        }

        return await SaveLogAsync(habitId, date);
    }

    public async Task<ServiceResult> UnlogAsync(int userId, int habitId, DateOnly date, DateOnly today)
    {
        var habit = await FindOwnedAsync(userId, habitId);
        if (habit is null)
        {
            return ServiceResult.NotFound();
        }

        var error = HabitRules.ValidateUnlogDate(date, today);
        if (error is not null)
        {
            return ServiceResult.Invalid(error);
        }

        // If there is no record for that day, nothing is deleted and the result is still Ok.
        await db.HabitLogs
            .Where(l => l.HabitId == habitId && l.Date == date)
            .ExecuteDeleteAsync();
        return ServiceResult.Ok();
    }

    // Returns the first rule the new habit breaks, or null when it can be created.
    private async Task<string?> ValidateNewHabitAsync(
        int userId, string? name, string? description, Frequency frequency, int? weeklyGoal)
    {
        var error = HabitRules.ValidateName(name)
            ?? HabitRules.ValidateDescription(description)
            ?? HabitRules.ValidateWeeklyGoal(frequency, weeklyGoal);
        if (error is not null)
        {
            return error;
        }

        var count = await db.Habits.CountAsync(h => h.UserId == userId);
        error = HabitRules.ValidateHabitCount(count);
        if (error is not null)
        {
            return error;
        }

        var cleanName = HabitRules.Normalize(name)!;
        return await NameIsTakenAsync(userId, cleanName, exceptHabitId: null) ? NameTakenMessage : null;
    }

    // Saves habit changes. Between our checks and this save, another request may have deleted
    // the habit (NotFound) or taken the same name (Invalid), so both cases are answered cleanly.
    private async Task<ServiceResult> SaveHabitChangesAsync()
    {
        try
        {
            await db.SaveChangesAsync();
            return ServiceResult.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return ServiceResult.NotFound();
        }
        catch (DbUpdateException ex) when (DatabaseErrors.IsUniqueViolation(ex))
        {
            db.ChangeTracker.Clear();
            return ServiceResult.Invalid(NameTakenMessage);
        }
    }

    // Inserts the record. If another request marked the same day first, the result is the same (Ok).
    // If the habit was deleted meanwhile, the foreign key rejects the insert (NotFound).
    private async Task<ServiceResult> SaveLogAsync(int habitId, DateOnly date)
    {
        db.HabitLogs.Add(new HabitLog { HabitId = habitId, Date = date });
        try
        {
            await db.SaveChangesAsync();
            return ServiceResult.Ok();
        }
        catch (DbUpdateException ex) when (DatabaseErrors.IsUniqueViolation(ex))
        {
            db.ChangeTracker.Clear();
            return ServiceResult.Ok();
        }
        catch (DbUpdateException ex) when (DatabaseErrors.IsForeignKeyViolation(ex))
        {
            db.ChangeTracker.Clear();
            return ServiceResult.NotFound();
        }
    }

    private Task<Habit?> FindOwnedAsync(int userId, int habitId) =>
        db.Habits.FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);

    // The database compares with lower("Name"); the parameter is lowered the same way for every culture.
    private Task<bool> NameIsTakenAsync(int userId, string name, int? exceptHabitId)
    {
        var lowered = name.ToLowerInvariant();
        var query = db.Habits.Where(h => h.UserId == userId && h.Name.ToLower() == lowered);
        if (exceptHabitId is int id)
        {
            query = query.Where(h => h.Id != id);
        }

        return query.AnyAsync();
    }
}
