using AsistentePersonal.Models;

namespace AsistentePersonal.Services;

// Pure rules for habits. No database, no clock: the caller passes "today" when it is needed.
// Each Validate* method returns null when the input is valid, or a message in Spanish when it is not.
public static class HabitRules
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 500;
    public const int MaxHabitsPerOwner = 50;
    public const int MinWeeklyGoal = 1;
    public const int MaxWeeklyGoal = 7;

    public static string? ValidateName(string? name)
    {
        var normalized = TextRules.Normalize(name);
        if (normalized is null)
        {
            return "El nombre es obligatorio.";
        }

        if (TextRules.CountChars(normalized) > MaxNameLength)
        {
            return $"El nombre no puede tener más de {MaxNameLength} caracteres.";
        }

        return null;
    }

    // The description is optional: a blank one is valid and is stored as "no description".
    public static string? ValidateDescription(string? description)
    {
        var normalized = TextRules.Normalize(description);
        if (normalized is null)
        {
            return null;
        }

        if (TextRules.CountChars(normalized) > MaxDescriptionLength)
        {
            return $"La descripción no puede tener más de {MaxDescriptionLength} caracteres.";
        }

        return null;
    }

    // A daily habit has no goal. A weekly habit must have a goal between 1 and 7 (spec D-3).
    public static string? ValidateWeeklyGoal(Frequency frequency, int? goal)
    {
        if (!Enum.IsDefined(frequency))
        {
            return "La frecuencia debe ser diaria o semanal.";
        }

        if (frequency == Frequency.Daily)
        {
            return null;
        }

        if (goal is null || goal < MinWeeklyGoal || goal > MaxWeeklyGoal)
        {
            return $"La meta semanal es obligatoria y debe estar entre {MinWeeklyGoal} y {MaxWeeklyGoal} días.";
        }

        return null;
    }

    public static string? ValidateHabitCount(int currentCount)
    {
        if (currentCount >= MaxHabitsPerOwner)
        {
            return $"Se alcanzó el máximo de {MaxHabitsPerOwner} hábitos.";
        }

        return null;
    }

    // Allowed: today and yesterday, and never before the habit existed.
    public static string? ValidateLogDate(DateOnly date, DateOnly today, DateOnly createdOn)
    {
        if (date > today)
        {
            return "No se puede registrar una fecha futura.";
        }

        if (date < today.AddDays(-1))
        {
            return "Solo se puede registrar hoy o ayer.";
        }

        if (date < createdOn)
        {
            return "El hábito todavía no existía ese día.";
        }

        return null;
    }

    // Allowed: today and yesterday. Older or future records cannot be changed.
    public static string? ValidateUnlogDate(DateOnly date, DateOnly today)
    {
        if (date > today || date < today.AddDays(-1))
        {
            return "Solo se pueden modificar registros de hoy o de ayer.";
        }

        return null;
    }
}
