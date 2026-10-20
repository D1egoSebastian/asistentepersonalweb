using AsistentePersonal.Models;

namespace AsistentePersonal.Services;

// One habit with the dates on which it was marked as done, in ascending order.
public sealed record HabitWithLogs(Habit Habit, IReadOnlyList<DateOnly> LogDates);

// What the habit list screen needs: the server's "today" and the owner's habits.
public sealed record HabitList(DateOnly Today, IReadOnlyList<HabitWithLogs> Habits);
