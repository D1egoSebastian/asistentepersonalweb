namespace AsistentePersonal.Models;

// A quick note from the owner. Its tags are stored in the idea itself (plan D-T1).
public class Idea
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Text { get; set; } = null!;
    public List<string> Tags { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Null while the idea is active. Setting it archives the idea (plan D-T4).
    public DateTimeOffset? ArchivedAt { get; set; }

    // PostgreSQL's row version (xmin): it changes on every write, so an edit can tell whether the idea
    // changed since it was opened (plan D-T12).
    public uint Version { get; set; }
}
