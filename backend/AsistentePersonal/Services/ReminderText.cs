namespace AsistentePersonal.Services;

// Pure texts of the daily digest: plain text, in neutral Spanish (spec 005, CA-1.6, RNF-8).
public static class ReminderText
{
    private const string Title = "Tu resumen de hoy";
    private const string IdeasHeading = "Ideas sin editar";
    private const string HabitsHeading = "Hábitos en riesgo";

    // "Tu resumen de hoy: 2 ideas y 1 hábito". A part with no items is left out.
    public static string Subject(int ideas, int habits)
    {
        var parts = new List<string>();
        if (ideas > 0)
        {
            parts.Add($"{ideas} {(ideas == 1 ? "idea" : "ideas")}");
        }

        if (habits > 0)
        {
            parts.Add($"{habits} {(habits == 1 ? "hábito" : "hábitos")}");
        }

        return parts.Count == 0 ? Title : $"{Title}: {string.Join(" y ", parts)}";
    }

    // One line per idea and per alert. A section with no lines is left out.
    public static string Body(IReadOnlyList<StaleIdea> ideas, IReadOnlyList<HabitAlert> alerts)
    {
        var sections = new List<string>();
        if (ideas.Count > 0)
        {
            sections.Add(Section(IdeasHeading, ideas.Select(IdeaLine)));
        }

        if (alerts.Count > 0)
        {
            sections.Add(Section(HabitsHeading, alerts.Select(AlertLine)));
        }

        return string.Join("\n\n", sections);
    }

    private static string Section(string heading, IEnumerable<string> lines) =>
        string.Join("\n", lines.Prepend(heading));

    private static string IdeaLine(StaleIdea idea) =>
        $"- \"{IdeaRules.Cut(idea.Text, ReminderRules.IdeaPreviewChars)}\" ({idea.Days} días)";

    private static string AlertLine(HabitAlert alert) => $"- {alert.HabitName}: {AlertSentence(alert)}";

    private static string AlertSentence(HabitAlert alert) => alert.Kind switch
    {
        AlertKind.MissedYesterday => "ayer no lo cumpliste.",
        AlertKind.StreakLost => $"perdiste una racha de {alert.Streak} días.",
        AlertKind.WeeklyAtRisk =>
            $"llevas {alert.Done} de {alert.Goal} esta semana y {DaysLeft(alert.DaysLeft)}.",
        AlertKind.LastWeekMissed => $"la semana pasada hiciste {alert.Done} de {alert.Goal}.",
        _ => throw new ArgumentOutOfRangeException(nameof(alert)),
    };

    private static string DaysLeft(int days) => days == 1 ? "queda 1 día" : $"quedan {days} días";
}
