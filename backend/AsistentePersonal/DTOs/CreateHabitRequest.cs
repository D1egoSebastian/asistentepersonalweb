using AsistentePersonal.Models;

namespace AsistentePersonal.DTOs;

// The owner and the creation date come from the server, never from the client.
public sealed record CreateHabitRequest(string? Name, string? Description, Frequency Frequency, int? WeeklyGoal);
