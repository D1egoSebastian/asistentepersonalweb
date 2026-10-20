namespace AsistentePersonal.DTOs;

// The date the user saw on screen (today or yesterday). Sent as "yyyy-MM-dd".
public sealed record LogRequest(DateOnly Date);
