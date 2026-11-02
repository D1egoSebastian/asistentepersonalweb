using System.Text.Json;
using AsistentePersonal.Models;
using AsistentePersonal.Services;

namespace AsistentePersonal.Tests;

public class ReminderRulesTests
{
    // ---- Shared cases (spec 005, D-3): the same JSON file is read by the frontend tests ----

    [Fact]
    public void SharedCases_DailyStreak()
    {
        var cases = LoadCases("dailyStreakCases");

        Assert.True(cases.Count >= 7);
        foreach (var testCase in cases)
        {
            var logs = testCase.GetProperty("logDates").EnumerateArray().Select(d => DateOnly.Parse(d.GetString()!)).ToHashSet();
            var today = DateOnly.Parse(testCase.GetProperty("today").GetString()!);

            Assert.True(
                testCase.GetProperty("dailyStreak").GetInt32() == ReminderRules.DailyStreak(logs, today),
                testCase.GetProperty("name").GetString());
        }
    }

    [Fact]
    public void SharedCases_WeekStart()
    {
        var cases = LoadCases("weekStartCases");

        Assert.True(cases.Count >= 5);
        foreach (var testCase in cases)
        {
            var date = DateOnly.Parse(testCase.GetProperty("date").GetString()!);

            Assert.True(
                DateOnly.Parse(testCase.GetProperty("weekStart").GetString()!) == ReminderRules.WeekStart(date),
                testCase.GetProperty("name").GetString());
        }
    }

    [Fact]
    public void Constants_MatchTheSpec()
    {
        Assert.Equal(14, ReminderRules.StaleIdeaDays);
        Assert.Equal(3, ReminderRules.MinLostStreak);
        Assert.Equal(80, ReminderRules.IdeaPreviewChars);
        Assert.Equal(new TimeOnly(8, 0), ReminderRules.DigestTime);
    }

    // ---- Habit alerts (spec 005, RF-3). Today is Friday 2026-10-16; yesterday is 2026-10-15. ----

    private static readonly DateOnly Today = new(2026, 10, 16);

    [Fact]
    public void MissedYesterday_DailyWithoutAYesterdayLog_Alerts()
    {
        var alerts = ReminderRules.HabitAlerts(Daily(), Logs(), Today);

        var alert = Assert.Single(alerts);
        Assert.Equal(AlertKind.MissedYesterday, alert.Kind);
        Assert.Equal("Correr", alert.HabitName);
    }

    [Fact]
    public void MissedYesterday_DailyWithAYesterdayLog_DoesNotAlert()
    {
        Assert.Empty(ReminderRules.HabitAlerts(Daily(), Logs("2026-10-15"), Today));
    }

    [Fact]
    public void MissedYesterday_CreatedYesterday_DoesNotAlert()
    {
        Assert.Empty(ReminderRules.HabitAlerts(Daily(createdOn: "2026-10-15"), Logs(), Today));
    }

    [Fact]
    public void MissedYesterday_CreatedTheDayBeforeYesterday_Alerts()
    {
        var alerts = ReminderRules.HabitAlerts(Daily(createdOn: "2026-10-14"), Logs(), Today);

        Assert.Equal(AlertKind.MissedYesterday, Assert.Single(alerts).Kind);
    }

    [Fact]
    public void MissedYesterday_Weekly_DoesNotAlert()
    {
        var alerts = ReminderRules.HabitAlerts(Weekly(3), Logs(), Today);

        Assert.DoesNotContain(alerts, a => a.Kind == AlertKind.MissedYesterday);
    }

    [Fact]
    public void StreakLost_FiveDayStreakCutYesterday_AlertsWithItsLength()
    {
        var logs = Logs("2026-10-10", "2026-10-11", "2026-10-12", "2026-10-13", "2026-10-14");

        var alerts = ReminderRules.HabitAlerts(Daily(), logs, Today);

        var lost = Assert.Single(alerts, a => a.Kind == AlertKind.StreakLost);
        Assert.Equal(5, lost.Streak);
    }

    [Fact]
    public void StreakLost_ThreeDays_IsTheMinimum()
    {
        var alerts = ReminderRules.HabitAlerts(Daily(), Logs("2026-10-12", "2026-10-13", "2026-10-14"), Today);

        Assert.Equal(3, Assert.Single(alerts, a => a.Kind == AlertKind.StreakLost).Streak);
    }

    [Fact]
    public void StreakLost_TwoDays_DoesNotAlert()
    {
        var alerts = ReminderRules.HabitAlerts(Daily(), Logs("2026-10-13", "2026-10-14"), Today);

        Assert.DoesNotContain(alerts, a => a.Kind == AlertKind.StreakLost);
        Assert.Single(alerts, a => a.Kind == AlertKind.MissedYesterday);
    }

    [Fact]
    public void StreakLost_YesterdayLogged_DoesNotAlert()
    {
        var logs = Logs("2026-10-12", "2026-10-13", "2026-10-14", "2026-10-15");

        Assert.Empty(ReminderRules.HabitAlerts(Daily(), logs, Today));
    }

    [Fact]
    public void StreakLost_Weekly_DoesNotAlert()
    {
        var alerts = ReminderRules.HabitAlerts(Weekly(3), Logs("2026-10-12", "2026-10-13", "2026-10-14"), Today);

        Assert.DoesNotContain(alerts, a => a.Kind == AlertKind.StreakLost);
    }

    [Fact]
    public void TwoAlerts_SameHabit()
    {
        var logs = Logs("2026-10-10", "2026-10-11", "2026-10-12", "2026-10-13", "2026-10-14");

        var alerts = ReminderRules.HabitAlerts(Daily(), logs, Today);

        Assert.Equal([AlertKind.MissedYesterday, AlertKind.StreakLost], alerts.Select(a => a.Kind));
        Assert.All(alerts, a => Assert.Equal("Correr", a.HabitName));
    }

    // ---- WeeklyAtRisk: goal 3, the week of 2026-10-12 (Monday) to 2026-10-18 (Sunday) ----

    [Fact]
    public void WeeklyAtRisk_FridayWithNoneOfThree_AlertsWithTheNumbers()
    {
        var alerts = ReminderRules.HabitAlerts(Weekly(3), Logs(), Today);

        var alert = Assert.Single(alerts, a => a.Kind == AlertKind.WeeklyAtRisk);
        Assert.Equal("Gimnasio", alert.HabitName);
        Assert.Equal((0, 3, 3), (alert.Done, alert.Goal, alert.DaysLeft));
    }

    [Fact]
    public void WeeklyAtRisk_MondayWithNoneOfThree_DoesNotAlert()
    {
        var alerts = ReminderRules.HabitAlerts(Weekly(3), Logs(), new DateOnly(2026, 10, 12));

        Assert.DoesNotContain(alerts, a => a.Kind == AlertKind.WeeklyAtRisk);
    }

    [Fact]
    public void WeeklyAtRisk_FridayWithOneOfThree_DoesNotAlert()
    {
        var alerts = ReminderRules.HabitAlerts(Weekly(3), Logs("2026-10-13"), Today);

        Assert.DoesNotContain(alerts, a => a.Kind == AlertKind.WeeklyAtRisk);
    }

    [Fact]
    public void WeeklyAtRisk_SaturdayWithOneOfThree_Alerts()
    {
        var alerts = ReminderRules.HabitAlerts(Weekly(3), Logs("2026-10-13"), new DateOnly(2026, 10, 17));

        var alert = Assert.Single(alerts, a => a.Kind == AlertKind.WeeklyAtRisk);
        Assert.Equal((1, 3, 2), (alert.Done, alert.Goal, alert.DaysLeft));
    }

    [Fact]
    public void WeeklyAtRisk_SundayWithNoneOfOne_Alerts()
    {
        var alerts = ReminderRules.HabitAlerts(Weekly(1), Logs(), new DateOnly(2026, 10, 18));

        var alert = Assert.Single(alerts, a => a.Kind == AlertKind.WeeklyAtRisk);
        Assert.Equal((0, 1, 1), (alert.Done, alert.Goal, alert.DaysLeft));
    }

    [Fact]
    public void WeeklyAtRisk_GoalMet_DoesNotAlert()
    {
        var alerts = ReminderRules.HabitAlerts(
            Weekly(3), Logs("2026-10-12", "2026-10-13", "2026-10-14"), new DateOnly(2026, 10, 18));

        Assert.DoesNotContain(alerts, a => a.Kind == AlertKind.WeeklyAtRisk);
    }

    [Fact]
    public void WeeklyAtRisk_TodayAlreadyLoggedCountsAsDone()
    {
        var saturday = new DateOnly(2026, 10, 17);

        var withoutToday = ReminderRules.HabitAlerts(Weekly(2), Logs(), saturday);
        var withToday = ReminderRules.HabitAlerts(Weekly(2), Logs("2026-10-17"), saturday);

        Assert.Single(withoutToday, a => a.Kind == AlertKind.WeeklyAtRisk);
        Assert.DoesNotContain(withToday, a => a.Kind == AlertKind.WeeklyAtRisk);
    }

    [Fact]
    public void WeeklyAtRisk_LogsOfThePreviousWeekDoNotCount()
    {
        var alerts = ReminderRules.HabitAlerts(Weekly(3), Logs("2026-10-05", "2026-10-06", "2026-10-07"), Today);

        var alert = Assert.Single(alerts, a => a.Kind == AlertKind.WeeklyAtRisk);
        Assert.Equal(0, alert.Done);
    }

    [Fact]
    public void WeeklyAtRisk_Daily_DoesNotAlert()
    {
        var alerts = ReminderRules.HabitAlerts(Daily(), Logs(), Today);

        Assert.DoesNotContain(alerts, a => a.Kind == AlertKind.WeeklyAtRisk);
    }

    // ---- LastWeekMissed: today is Monday 2026-10-19; the week before is 2026-10-12 to 2026-10-18 ----

    private static readonly DateOnly NextMonday = new(2026, 10, 19);

    [Fact]
    public void LastWeekMissed_OneOfThree_AlertsWithTheNumbers()
    {
        var alerts = ReminderRules.HabitAlerts(Weekly(3), Logs("2026-10-14"), NextMonday);

        var alert = Assert.Single(alerts, a => a.Kind == AlertKind.LastWeekMissed);
        Assert.Equal("Gimnasio", alert.HabitName);
        Assert.Equal((1, 3), (alert.Done, alert.Goal));
    }

    [Fact]
    public void LastWeekMissed_NoLogsAtAll_Alerts()
    {
        var alerts = ReminderRules.HabitAlerts(Weekly(3), Logs(), NextMonday);

        Assert.Equal(0, Assert.Single(alerts, a => a.Kind == AlertKind.LastWeekMissed).Done);
    }

    [Fact]
    public void LastWeekMissed_NotAMonday_DoesNotAlert()
    {
        var alerts = ReminderRules.HabitAlerts(Weekly(3), Logs("2026-10-14"), new DateOnly(2026, 10, 20));

        Assert.DoesNotContain(alerts, a => a.Kind == AlertKind.LastWeekMissed);
    }

    [Fact]
    public void LastWeekMissed_GoalMetLastWeek_DoesNotAlert()
    {
        var alerts = ReminderRules.HabitAlerts(
            Weekly(3), Logs("2026-10-12", "2026-10-14", "2026-10-18"), NextMonday);

        Assert.DoesNotContain(alerts, a => a.Kind == AlertKind.LastWeekMissed);
    }

    [Fact]
    public void LastWeekMissed_OnlyLogsOfLastWeekCount()
    {
        var alerts = ReminderRules.HabitAlerts(
            Weekly(2), Logs("2026-10-10", "2026-10-11", "2026-10-19"), NextMonday);

        Assert.Equal(0, Assert.Single(alerts, a => a.Kind == AlertKind.LastWeekMissed).Done);
    }

    [Fact]
    public void LastWeekMissed_CreatedThisWeek_DoesNotAlert()
    {
        var alerts = ReminderRules.HabitAlerts(Weekly(3, createdOn: "2026-10-19"), Logs(), NextMonday);

        Assert.DoesNotContain(alerts, a => a.Kind == AlertKind.LastWeekMissed);
    }

    [Fact]
    public void LastWeekMissed_CreatedOnWednesdayOfLastWeek_DoesNotAlert()
    {
        var alerts = ReminderRules.HabitAlerts(Weekly(3, createdOn: "2026-10-14"), Logs(), NextMonday);

        Assert.DoesNotContain(alerts, a => a.Kind == AlertKind.LastWeekMissed);
    }

    [Fact]
    public void LastWeekMissed_CreatedOnLastMonday_DoesNotAlert()
    {
        var alerts = ReminderRules.HabitAlerts(Weekly(3, createdOn: "2026-10-12"), Logs(), NextMonday);

        Assert.DoesNotContain(alerts, a => a.Kind == AlertKind.LastWeekMissed);
    }

    [Fact]
    public void LastWeekMissed_CreatedTheSundayBeforeLastWeek_Alerts()
    {
        var alerts = ReminderRules.HabitAlerts(Weekly(3, createdOn: "2026-10-11"), Logs(), NextMonday);

        Assert.Single(alerts, a => a.Kind == AlertKind.LastWeekMissed);
    }

    private static Habit Daily(string createdOn = "2026-08-31") => new()
    {
        Id = 1, UserId = 1, Name = "Correr", Frequency = Frequency.Daily, CreatedOn = DateOnly.Parse(createdOn),
    };

    private static Habit Weekly(int goal, string createdOn = "2026-08-31") => new()
    {
        Id = 2, UserId = 1, Name = "Gimnasio", Frequency = Frequency.Weekly, WeeklyGoal = goal, CreatedOn = DateOnly.Parse(createdOn),
    };

    private static HashSet<DateOnly> Logs(params string[] dates) => dates.Select(DateOnly.Parse).ToHashSet();

    private static List<JsonElement> LoadCases(string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "test-cases", "daily-streak.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty(name).EnumerateArray().Select(e => e.Clone()).ToList();
    }
}
