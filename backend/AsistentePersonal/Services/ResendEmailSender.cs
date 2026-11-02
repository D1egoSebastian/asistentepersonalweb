using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace AsistentePersonal.Services;

// Sends the digest through Resend's HTTPS API: no package, only HttpClient (spec 005, RNF-7).
// The HttpClient comes configured from Program.cs (base address, API key, timeout). The key and the
// response are never logged: only the status code or the kind of error (RNF-2).
public class ResendEmailSender(HttpClient http, ILogger<ResendEmailSender> logger) : IEmailSender
{
    // Resend's test sender: it only delivers to the address of the Resend account (plan D-T18).
    private const string From = "onboarding@resend.dev";

    // The Idempotency-Key header is not sent: Resend's support for it is still unverified (task T04).
    public async Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var body = new { from = From, to = new[] { message.To }, subject = message.Subject, text = message.Text };
        try
        {
            using var response = await http.PostAsJsonAsync("emails", body, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Resend answered with status {StatusCode}.", (int)response.StatusCode);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning("The request to Resend failed: {ErrorType}.", ex.GetType().Name);
            return false;
        }
    }
}
