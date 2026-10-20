namespace AsistentePersonal.Models;

// The owner's account. There is only one row, always with Id = 1 (spec 002, plan §0).
public class User
{
    public const int OwnerId = 1;

    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}
