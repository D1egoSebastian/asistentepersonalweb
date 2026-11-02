using AsistentePersonal.Services;

namespace AsistentePersonal.Tests;

public class ReminderTextTests
{
    // ---- Subject (spec 005, CA-1.6) ----

    [Theory]
    [InlineData(2, 1, "Tu resumen de hoy: 2 ideas y 1 hábito")]
    [InlineData(1, 2, "Tu resumen de hoy: 1 idea y 2 hábitos")]
    [InlineData(1, 0, "Tu resumen de hoy: 1 idea")]
    [InlineData(0, 3, "Tu resumen de hoy: 3 hábitos")]
    [InlineData(0, 0, "Tu resumen de hoy")]
    public void Subject_CountsIdeasAndHabits(int ideas, int habits, string expected)
    {
        Assert.Equal(expected, ReminderText.Subject(ideas, habits));
    }

    // ---- Body (CA-1.6, CA-2.4, CA-3.5) ----

    [Fact]
    public void IdeaLine_CutsTheTextTo80CharactersAndShowsTheDays()
    {
        var idea = new StaleIdea(new string('a', 100), 15);

        var body = ReminderText.Body([idea], []);

        Assert.Equal($"Ideas sin editar\n- \"{new string('a', 80)}…\" (15 días)", body);
    }

    [Fact]
    public void IdeaLine_UsesTheFirstLineWithText()
    {
        var idea = new StaleIdea("\nRegalo para mamá\nUn reloj", 28);

        Assert.Equal("Ideas sin editar\n- \"Regalo para mamá\" (28 días)", ReminderText.Body([idea], []));
    }

    [Fact]
    public void OneLinePerAlert()
    {
        var alerts = new[]
        {
            new HabitAlert(AlertKind.MissedYesterday, "Daily coding challenge"),
            new HabitAlert(AlertKind.StreakLost, "Daily coding challenge", Streak: 5),
            new HabitAlert(AlertKind.WeeklyAtRisk, "Gimnasio", Done: 0, Goal: 3, DaysLeft: 3),
            new HabitAlert(AlertKind.LastWeekMissed, "Leer", Done: 1, Goal: 4),
        };

        var lines = ReminderText.Body([], alerts).Split('\n');

        Assert.Equal(
            [
                "Hábitos en riesgo",
                "- Daily coding challenge: ayer no lo cumpliste.",
                "- Daily coding challenge: perdiste una racha de 5 días.",
                "- Gimnasio: llevas 0 de 3 esta semana y quedan 3 días.",
                "- Leer: la semana pasada hiciste 1 de 4.",
            ],
            lines);
    }

    [Fact]
    public void WeeklyAtRisk_WithOneDayLeft_UsesTheSingular()
    {
        var alert = new HabitAlert(AlertKind.WeeklyAtRisk, "Gimnasio", Done: 2, Goal: 3, DaysLeft: 1);

        Assert.Equal("Hábitos en riesgo\n- Gimnasio: llevas 2 de 3 esta semana y queda 1 día.", ReminderText.Body([], [alert]));
    }

    [Fact]
    public void Body_HasBothSectionsSeparatedByABlankLine()
    {
        var ideas = new[]
        {
            new StaleIdea("Aprender Rust para el backend de la app de música, empezando por el principio de todo y luego el resto", 15),
            new StaleIdea("Regalo para mamá", 28),
        };
        var alerts = new[]
        {
            new HabitAlert(AlertKind.MissedYesterday, "Daily coding challenge"),
            new HabitAlert(AlertKind.StreakLost, "Daily coding challenge", Streak: 5),
            new HabitAlert(AlertKind.WeeklyAtRisk, "Gimnasio", Done: 0, Goal: 3, DaysLeft: 3),
        };

        var expected = string.Join("\n",
            "Ideas sin editar",
            "- \"Aprender Rust para el backend de la app de música, empezando por el principio de…\" (15 días)",
            "- \"Regalo para mamá\" (28 días)",
            "",
            "Hábitos en riesgo",
            "- Daily coding challenge: ayer no lo cumpliste.",
            "- Daily coding challenge: perdiste una racha de 5 días.",
            "- Gimnasio: llevas 0 de 3 esta semana y quedan 3 días.");

        Assert.Equal(expected, ReminderText.Body(ideas, alerts));
    }
}
