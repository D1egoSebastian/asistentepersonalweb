namespace AsistentePersonal.Services;

// IdempotencyKey identifies the digest of one day, so a provider that supports it can ignore a repeated send.
public sealed record EmailMessage(string To, string Subject, string Text, string IdempotencyKey);

public interface IEmailSender
{
    // True when the provider accepted the message. Never throws for network or HTTP errors.
    Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
