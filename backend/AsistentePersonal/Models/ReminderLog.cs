namespace AsistentePersonal.Models;

// An idea that was included in a digest on SentOn. It lives apart from Idea because writing to Idea
// would change its UpdatedAt and its Version (plan D-T3).
public class ReminderLog
{
    public int Id { get; set; }
    public int IdeaId { get; set; }
    public DateOnly SentOn { get; set; }
}
