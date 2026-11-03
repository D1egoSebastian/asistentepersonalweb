using System.Text.Json;
using AsistentePersonal.Models;
using AsistentePersonal.Services;

namespace AsistentePersonal.Tests;

public class ReminderRulesTests
{
    // ---- Shared cases (spec 005, D-3): the same JSON file is read by the frontend tests ----

    // Each case is reported on its own, so one failure never hides the others.
    public static TheoryData<string, string, string[], int> DailyStreakCases()
    {
        var data = new TheoryData<string, string, string[], int>();
        foreach (var testCase in LoadCases("dailyStreakCases"))
        {
            data.Add(
                testCase.GetProperty("name").GetString()!,
                testCase.GetProperty("today").GetString()!,
                testCase.GetProperty("logDates").EnumerateArray().Select(d => d.GetString()!).ToArray(),
                testCase.GetProperty("dailyStreak").GetInt32());
        }

        return data;
    }

    public static TheoryData<string, string, string> WeekStartCases()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var testCase in LoadCases("weekStartCases"))
        {
            data.Add(
                testCase.GetProperty("name").GetString()!,
                testCase.GetProperty("date").GetString()!,
                testCase.GetProperty("weekStart").GetString()!);
        }

        return data;
    }

    [Fact]
    public void SharedCases_AreAllLoaded()
    {
        Assert.True(LoadCases("dailyStreakCases").Count >= 8);
        Assert.True(LoadCases("weekStartCases").Count >= 5);
    }

    [Theory]
    [MemberData(nameof(DailyStreakCases))]
    public void SharedCases_DailyStreak(string name, string today, string[] logDates, int expected)
    {
        var logs = logDates.Select(DateOnly.Parse).ToHashSet();

        Assert.True(expected == ReminderRules.DailyStreak(logs, DateOnly.Parse(today)), name);
    }

    [Theory]
    [MemberData(nameof(WeekStartCases))]
    public void SharedCases_WeekStart(string name, string date, string expected)
    {
        Assert.True(DateOnly.Parse(expected) == ReminderRules.WeekStart(DateOnly.Parse(date)), name);
    }

    // ---- Digest time, secret and stale ideas ----

    [Theory]
    [InlineData(7, 59, false)]
    [InlineData(0, 0, false)]
    [InlineData(8, 0, true)]
    [InlineData(23, 59, true)]
    public void IsDigestTime_IsTrueFromEightOnward(int hour, int minute, bool expected)
    {
        Assert.Equal(expected, ReminderRules.IsDigestTime(new TimeOnly(hour, minute)));
    }

    [Fact]
    public void IsValidSecret_AcceptsTheSameValue()
    {
        Assert.True(ReminderRules.IsValidSecret("a-long-shared-secret-of-32-characters", "a-long-shared-secret-of-32-characters"));
    }

    [Theory]
    [InlineData("a-long-shared-secret-of-32-characterz")]
    [InlineData("short")]
    [InlineData("a-long-shared-secret-of-32-characters-and-more")]
    [InlineData(null)]
    [InlineData("")]
    public void IsValidSecret_RejectsAnythingElse(string? received)
    {
        Assert.False(ReminderRules.IsValidSecret(received, "a-long-shared-secret-of-32-characters"));
    }

    [Fact]
    public void IdeaDue_At14Days_NotAt13()
    {
        Assert.True(ReminderRules.IsIdeaDue(new DateOnly(2026, 10, 2), null, Today));
        Assert.False(ReminderRules.IsIdeaDue(new DateOnly(2026, 10, 3), null, Today));
    }

    [Fact]
    public void IdeaDue_14DaysAfterLastReminder()
    {
        var editedLongAgo = new DateOnly(2026, 8, 1);

        Assert.False(ReminderRules.IsIdeaDue(editedLongAgo, new DateOnly(2026, 10, 3), Today));
        Assert.True(ReminderRules.IsIdeaDue(editedLongAgo, new DateOnly(2026, 10, 2), Today));
    }

    [Fact]
    public void IdeaEditedAfterReminder_CountsFromEdit()
    {
        var remindedLongAgo = new DateOnly(2026, 9, 1);

        Assert.False(ReminderRules.IsIdeaDue(new DateOnly(2026, 10, 10), remindedLongAgo, Today));
        Assert.True(ReminderRules.IsIdeaDue(new DateOnly(2026, 10, 2), remindedLongAgo, Today));
    }

    [Fact]
    public void DaysSince_CountsCalendarDays()
    {
        Assert.Equal(15, ReminderRules.DaysSince(new DateOnly(2026, 10, 1), Today));
        Assert.Equal(28, ReminderRules.DaysSince(new DateOnly(2026, 9, 18), Today));
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

    [Fact]
    public void WeeklyAtRisk_CreatedThisWeek_DoesNotAlert()
    {
        var alerts = ReminderRules.HabitAlerts(Weekly(3, createdOn: "2026-10-14"), Logs(), Today);

        Assert.DoesNotContain(alerts, a => a.Kind == AlertKind.WeeklyAtRisk);
    }

    [Fact]
    public void WeeklyAtRisk_CreatedLastWeek_Alerts()
    {
        var alerts = ReminderRules.HabitAlerts(Weekly(3, createdOn: "2026-10-08"), Logs(), Today);

        Assert.Single(alerts, a => a.Kind == AlertKind.WeeklyAtRisk);
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
