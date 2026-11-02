namespace AsistentePersonal.Models;

// One row per day with a digest sent. ForDate is the primary key, so a second digest for the same day
// is rejected by the database (spec 005, RNF-3).
public class DailyDigestLog
{
    public DateOnly ForDate { get; set; }
    public DateTimeOffset SentAt { get; set; }
}
