using System.Text.Json;
using AsistentePersonal.Controllers;
using AsistentePersonal.Data;
using AsistentePersonal.Models;
using AsistentePersonal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AsistentePersonal.Tests;

// The endpoint called by the scheduled flow (spec 005, RF-4). The controller is built by hand with a
// DefaultHttpContext and no session. The clock is fixed at 23:00 UTC, which is past 8:00 in every time zone.
public class RemindersControllerTests : IDisposable
{
    private const string Secret = "a-long-shared-secret-of-32-characters";

    private static readonly DateTimeOffset Now = new(2026, 10, 16, 23, 0, 0, TimeSpan.Zero);

    private readonly string? _previousZone = Environment.GetEnvironmentVariable(OwnerClock.TimeZoneVariable);

    public RemindersControllerTests()
    {
        Environment.SetEnvironmentVariable(OwnerClock.TimeZoneVariable, "America/La_Paz");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(OwnerClock.TimeZoneVariable, _previousZone);
    }

    // ---- CA-4.2, CA-4.3: no secret, no digest ----

    [Fact]
    public async Task MissingOrWrongSecret_Returns401_AndSendsNothing()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerWithStaleIdeaAsync(db);
        var sender = new FakeEmailSender();
        var controller = NewController(db, sender);

        foreach (var received in new string?[] { null, "", "not-the-secret" })
        {
            var result = await controller.Daily(received);

            Assert.IsType<UnauthorizedResult>(result);
        }

        Assert.Empty(sender.Messages);
        Assert.Equal(0, await db.DailyDigestLogs.CountAsync());

        // The same data does send with the right secret, so the silence above comes from the secret.
        await controller.Daily(Secret);
        Assert.Single(sender.Messages);
    }

    // These two checks guard what the unit tests above cannot see, because they skip the MVC pipeline.

    [Fact]
    public void IsExplicitlyAnonymous_SoTheSecretIsTheOnlyDoor()
    {
        Assert.NotNull(Attribute.GetCustomAttribute(typeof(RemindersController), typeof(AllowAnonymousAttribute)));
        Assert.Null(Attribute.GetCustomAttribute(typeof(RemindersController), typeof(AuthorizeAttribute)));
    }

    [Fact]
    public void IsNotAnApiController_SoTheRejectionKeepsAnEmptyBody()
    {
        // [ApiController] would turn the bare 401 into a ProblemDetails body (spec 005, CA-4.2).
        Assert.Null(Attribute.GetCustomAttribute(typeof(RemindersController), typeof(ApiControllerAttribute)));
    }

    // ---- CA-4.1, CA-4.4: the answer says what happened and how many items, nothing else ----

    [Fact]
    public async Task ValidSecret_Returns200()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerWithStaleIdeaAsync(db);

        var result = await NewController(db, new FakeEmailSender()).Daily(Secret);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    }

    [Fact]
    public async Task Body_HasOnlyResultAndItems()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerWithStaleIdeaAsync(db);
        var controller = NewController(db, new FakeEmailSender());

        var sent = Assert.IsType<OkObjectResult>(await controller.Daily(Secret));
        var skipped = Assert.IsType<OkObjectResult>(await controller.Daily(Secret));

        // The idea text ("Regalo para mamá") and the owner's address never travel in the answer.
        Assert.Equal("{\"result\":\"sent\",\"items\":1}", ToJson(sent.Value));
        Assert.Equal("{\"result\":\"skipped\",\"items\":0}", ToJson(skipped.Value));
    }

    [Fact]
    public async Task Failed_Returns502()
    {
        await using var db = await TestDatabase.CreateEmptyContextAsync();
        await AddOwnerWithStaleIdeaAsync(db);
        var sender = new FakeEmailSender { Accepts = false };

        var result = await NewController(db, sender).Daily(Secret);

        var failed = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, failed.StatusCode);
        Assert.Equal("{\"result\":\"failed\",\"items\":1}", ToJson(failed.Value));
    }

    // ---- Helpers ----

    private static RemindersController NewController(AppDbContext db, FakeEmailSender sender) =>
        new(new ReminderService(db, sender), new ReminderSecret(Secret), new FixedTimeProvider(Now))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private static string ToJson(object? value) => JsonSerializer.Serialize(value, WebJson);

    private static async Task AddOwnerWithStaleIdeaAsync(AppDbContext db)
    {
        db.Users.Add(new User { Id = User.OwnerId, Email = "dueno@ejemplo.com", PasswordHash = "hash-de-prueba", CreatedAt = Now.AddDays(-90) });
        db.Ideas.Add(new Idea
        {
            UserId = User.OwnerId, Text = "Regalo para mamá", CreatedAt = Now.AddDays(-40), UpdatedAt = Now.AddDays(-40),
        });
        await db.SaveChangesAsync();
    }

    // A clock that always says the same moment.
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
