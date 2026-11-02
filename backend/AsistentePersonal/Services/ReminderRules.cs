using AsistentePersonal.Models;

namespace AsistentePersonal.Services;

// Pure rules for the daily digest (spec 005). No database, no clock: "today" always comes as a parameter.
// Weeks run Monday to Sunday, like weekStart in the frontend.
public static class ReminderRules
{
    public const int StaleIdeaDays = 14;
    public const int MinLostStreak = 3;
    public const int IdeaPreviewChars = 80;
    public static readonly TimeOnly DigestTime = new(8, 0);

    // Monday of the week that contains the date. Ported from weekStart in frontend/src/logic/dates.js.
    public static DateOnly WeekStart(DateOnly date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-daysSinceMonday);
    }

    // Consecutive marked days, ending today. If today is not marked yet, the streak ends yesterday.
    // Ported from dailyStreak in frontend/src/logic/streak.js.
    public static int DailyStreak(IReadOnlySet<DateOnly> logs, DateOnly today)
    {
        var cursor = logs.Contains(today) ? today : today.AddDays(-1);
        var streak = 0;
        while (logs.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    // The alerts of one habit for today: zero, one or two lines for the digest (spec 005, RF-3).
    public static IReadOnlyList<HabitAlert> HabitAlerts(Habit habit, IReadOnlySet<DateOnly> logs, DateOnly today)
    {
        return habit.Frequency == Frequency.Daily ? DailyAlerts(habit, logs, today).ToList() : [];
    }

    // Yesterday was not logged: the habit missed a day, and a streak of 3 days or more may have ended with it.
    private static IEnumerable<HabitAlert> DailyAlerts(Habit habit, IReadOnlySet<DateOnly> logs, DateOnly today)
    {
        var yesterday = today.AddDays(-1);
        if (logs.Contains(yesterday))
        {
            yield break;
        }

        // The habit must have existed the whole of yesterday (plan D-T9).
        if (habit.CreatedOn < yesterday)
        {
            yield return new HabitAlert(AlertKind.MissedYesterday, habit.Name);
        }

        // Yesterday is not logged, so the count starts the day before yesterday.
        var lost = DailyStreak(logs, yesterday);
        if (lost >= MinLostStreak)
        {
            yield return new HabitAlert(AlertKind.StreakLost, habit.Name, Streak: lost);
        }
    }
}

public enum AlertKind
{
    MissedYesterday,
    StreakLost,
}

// One line of the digest about a habit. Streak is only used by StreakLost.
public sealed record HabitAlert(AlertKind Kind, string HabitName, int Streak = 0);
