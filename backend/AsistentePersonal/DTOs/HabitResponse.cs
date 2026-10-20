using AsistentePersonal.Models;

namespace AsistentePersonal.DTOs;

public sealed record HabitResponse(
    int Id,
    string Name,
    string? Description,
    Frequency Frequency,
    int? WeeklyGoal,
    DateOnly CreatedOn,
    IReadOnlyList<DateOnly> LogDates);
