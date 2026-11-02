using AsistentePersonal.Services;

namespace AsistentePersonal.Tests;

// A hand-written replacement for the email provider: it keeps every message it receives and answers
// with the configured result. Tests use it instead of a mocking library (constitution, principle 4).
public sealed class FakeEmailSender : IEmailSender
{
    public List<EmailMessage> Messages { get; } = [];

    public bool Accepts { get; set; } = true;

    public Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        Messages.Add(message);
        return Task.FromResult(Accepts);
    }
}
