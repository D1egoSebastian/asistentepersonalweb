namespace AsistentePersonal.DTOs;

// The only thing the reminders endpoint answers (spec 005, CA-4.4): what happened ("sent", "skipped" or "failed")
// and how many ideas and habits it included. Never the text of an idea or the name of a habit.
public sealed record ReminderResponse(string Result, int Items);
