namespace AsistentePersonal.DTOs;

// Only name and description can change. Frequency and goal are fixed at creation (spec RF-8).
public sealed record UpdateHabitRequest(string? Name, string? Description);
