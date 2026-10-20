namespace AsistentePersonal.Models;

public class Habit
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Frequency Frequency { get; set; }
    public int? WeeklyGoal { get; set; }
    public DateOnly CreatedOn { get; set; }
}
