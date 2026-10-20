namespace AsistentePersonal.DTOs;

public sealed record SetupRequest(string? Email, string? Password, string? PasswordConfirmation);

public sealed record LoginRequest(string? Email, string? Password);

// Only says whether the owner's account exists. It never reveals the email.
public sealed record AuthStatusResponse(bool HasOwner);

public sealed record SessionResponse(string Token, DateTimeOffset ExpiresAt);
