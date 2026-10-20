namespace AsistentePersonal.DTOs;

// "today" is sent by the server so the screen never decides the date on its own.
public sealed record HabitListResponse(DateOnly Today, IReadOnlyList<HabitResponse> Habits);
