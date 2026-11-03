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

    // Idempotency-Key makes Resend ignore a repeated send of the same digest within 24 hours (task T04).
    public async Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var body = new { from = From, to = new[] { message.To }, subject = message.Subject, text = message.Text };
        using var request = new HttpRequestMessage(HttpMethod.Post, "emails") { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", message.IdempotencyKey);
        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
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
