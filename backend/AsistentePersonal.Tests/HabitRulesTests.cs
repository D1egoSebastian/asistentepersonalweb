using AsistentePersonal.Models;
using AsistentePersonal.Services;

namespace AsistentePersonal.Tests;

public class HabitRulesTests
{
    // Fixed "today" so the tests never depend on the real clock.
    private static readonly DateOnly Today = new(2026, 10, 16);

    // ---- T09: name and description ----

    [Fact]
    public void ValidateName_ReturnsError_WhenBlank()
    {
        Assert.NotNull(HabitRules.ValidateName(null));
        Assert.NotNull(HabitRules.ValidateName("   "));
    }

    [Fact]
    public void ValidateName_AcceptsExactly100Characters()
    {
        Assert.Null(HabitRules.ValidateName(new string('a', 100)));
    }

    [Fact]
    public void ValidateName_RejectsMoreThan100Characters()
    {
        Assert.NotNull(HabitRules.ValidateName(new string('a', 101)));
    }

    [Fact]
    public void ValidateName_MeasuresLengthAfterTrimming()
    {
        // 100 letters plus spaces around them: the spaces do not count
        Assert.Null(HabitRules.ValidateName("  " + new string('a', 100) + "  "));
    }

    [Fact]
    public void ValidateDescription_AcceptsNull_BecauseItIsOptional()
    {
        Assert.Null(HabitRules.ValidateDescription(null));
    }

    [Fact]
    public void ValidateDescription_AcceptsExactly500Characters()
    {
        Assert.Null(HabitRules.ValidateDescription(new string('a', 500)));
    }

    [Fact]
    public void ValidateDescription_RejectsMoreThan500Characters()
    {
        Assert.NotNull(HabitRules.ValidateDescription(new string('a', 501)));
    }

    // ---- T10: weekly goal and habit count ----

    [Fact]
    public void ValidateWeeklyGoal_RejectsMissingGoal_ForWeeklyHabit()
    {
        Assert.NotNull(HabitRules.ValidateWeeklyGoal(Frequency.Weekly, null));
    }

    [Fact]
    public void ValidateWeeklyGoal_RejectsZero_AndEight()
    {
        Assert.NotNull(HabitRules.ValidateWeeklyGoal(Frequency.Weekly, 0));
        Assert.NotNull(HabitRules.ValidateWeeklyGoal(Frequency.Weekly, 8));
    }

    [Fact]
    public void ValidateWeeklyGoal_AcceptsOneAndSeven()
    {
        Assert.Null(HabitRules.ValidateWeeklyGoal(Frequency.Weekly, 1));
        Assert.Null(HabitRules.ValidateWeeklyGoal(Frequency.Weekly, 7));
    }

    [Fact]
    public void ValidateWeeklyGoal_RejectsAnUnknownFrequency()
    {
        Assert.NotNull(HabitRules.ValidateWeeklyGoal((Frequency)99, null));
    }

    [Fact]
    public void ValidateWeeklyGoal_AcceptsNoGoal_ForDailyHabit()
    {
        Assert.Null(HabitRules.ValidateWeeklyGoal(Frequency.Daily, null));
    }

    [Fact]
    public void ValidateWeeklyGoal_IgnoresGoal_ForDailyHabit()
    {
        // A daily habit ignores any goal sent; the service stores it without a goal
        Assert.Null(HabitRules.ValidateWeeklyGoal(Frequency.Daily, 5));
    }

    [Fact]
    public void ValidateHabitCount_AcceptsFortyNineHabits_RejectsFifty()
    {
        Assert.Null(HabitRules.ValidateHabitCount(49));
        Assert.NotNull(HabitRules.ValidateHabitCount(50));
    }

    // ---- T11: log date ----

    [Fact]
    public void ValidateLogDate_RejectsFutureDate()
    {
        Assert.NotNull(HabitRules.ValidateLogDate(Today.AddDays(1), Today, Today.AddDays(-10)));
    }

    [Fact]
    public void ValidateLogDate_AcceptsToday()
    {
        Assert.Null(HabitRules.ValidateLogDate(Today, Today, Today.AddDays(-10)));
    }

    [Fact]
    public void ValidateLogDate_AcceptsYesterday()
    {
        Assert.Null(HabitRules.ValidateLogDate(Today.AddDays(-1), Today, Today.AddDays(-10)));
    }

    [Fact]
    public void ValidateLogDate_RejectsDayBeforeYesterday()
    {
        Assert.NotNull(HabitRules.ValidateLogDate(Today.AddDays(-2), Today, Today.AddDays(-10)));
    }

    [Fact]
    public void ValidateLogDate_RejectsYesterday_WhenHabitWasCreatedToday()
    {
        Assert.NotNull(HabitRules.ValidateLogDate(Today.AddDays(-1), Today, Today));
    }

    // ---- T12: unlog date ----

    [Fact]
    public void ValidateUnlogDate_AcceptsTodayAndYesterday()
    {
        Assert.Null(HabitRules.ValidateUnlogDate(Today, Today));
        Assert.Null(HabitRules.ValidateUnlogDate(Today.AddDays(-1), Today));
    }

    [Fact]
    public void ValidateUnlogDate_RejectsDayBeforeYesterday_AndFutureDate()
    {
        Assert.NotNull(HabitRules.ValidateUnlogDate(Today.AddDays(-2), Today));
        Assert.NotNull(HabitRules.ValidateUnlogDate(Today.AddDays(1), Today));
    }
}
