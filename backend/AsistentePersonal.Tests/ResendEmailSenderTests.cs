using System.Net;
using System.Text.Json;
using AsistentePersonal.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AsistentePersonal.Tests;

// ResendEmailSender talks to the provider through HttpClient. The tests replace the network with a
// hand-written handler: no package, and no real email (spec 005, RNF-7, plan D-T6).
public class ResendEmailSenderTests
{
    private static readonly EmailMessage Message =
        new("dueno@ejemplo.com", "Tu resumen de hoy: 1 idea", "Ideas sin editar\n- \"Regalo\" (15 días)", "digest-2026-10-16");

    [Fact]
    public async Task Posts_FromToSubjectAndText_AsJson()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        await NewSender(handler).SendAsync(Message, CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://api.resend.com/emails", handler.Uri);
        using var body = JsonDocument.Parse(handler.Body!);
        var root = body.RootElement;
        Assert.Equal(["from", "to", "subject", "text"], root.EnumerateObject().Select(p => p.Name));
        Assert.Equal("onboarding@resend.dev", root.GetProperty("from").GetString());
        Assert.Equal(["dueno@ejemplo.com"], root.GetProperty("to").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(Message.Subject, root.GetProperty("subject").GetString());
        Assert.Equal(Message.Text, root.GetProperty("text").GetString());
    }

    [Fact]
    public async Task SendsTheMessageKey_InIdempotencyKeyHeader()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        await NewSender(handler).SendAsync(Message, CancellationToken.None);

        Assert.Equal("digest-2026-10-16", handler.IdempotencyKey);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, true)]
    [InlineData(HttpStatusCode.Accepted, true)]
    [InlineData(HttpStatusCode.UnprocessableEntity, false)]
    [InlineData(HttpStatusCode.TooManyRequests, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    public async Task ReturnsTrueOnlyForASuccessStatus(HttpStatusCode status, bool expected)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(status));

        Assert.Equal(expected, await NewSender(handler).SendAsync(Message, CancellationToken.None));
    }

    [Fact]
    public async Task NetworkError_ReturnsFalse_InsteadOfThrowing()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("no connection"));

        Assert.False(await NewSender(handler).SendAsync(Message, CancellationToken.None));
    }

    [Fact]
    public async Task Timeout_ReturnsFalse_InsteadOfThrowing()
    {
        var handler = new StubHandler(_ => throw new TaskCanceledException("timeout", new TimeoutException()));

        Assert.False(await NewSender(handler).SendAsync(Message, CancellationToken.None));
    }

    private static ResendEmailSender NewSender(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") }, NullLogger<ResendEmailSender>.Instance);

    // Answers every request with the given function and remembers the first one.
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }

        public string? Uri { get; private set; }

        public string? Body { get; private set; }

        public string? IdempotencyKey { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Uri = request.RequestUri?.ToString();
            IdempotencyKey = request.Headers.TryGetValues("Idempotency-Key", out var keys) ? keys.Single() : null;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return respond(request);
        }
    }
}
