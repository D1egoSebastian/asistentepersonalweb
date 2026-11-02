using AsistentePersonal.DTOs;
using AsistentePersonal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsistentePersonal.Controllers;

// The only endpoint that works without the owner's session: the scheduled flow has no account, so it proves
// itself with a shared secret (constitution, principle 5; spec 005, S-4). It validates the secret and delegates.
// It is not an [ApiController] on purpose: that attribute would turn the bare 401 into a ProblemDetails body,
// and a rejected call must reveal nothing (CA-4.2).
[Route("api/reminders")]
[AllowAnonymous]
public class RemindersController(ReminderService reminders, ReminderSecret secret, TimeProvider clock) : ControllerBase
{
    [HttpPost("daily")]
    public async Task<IActionResult> Daily([FromHeader(Name = "X-Reminder-Secret")] string? received)
    {
        if (!ReminderRules.IsValidSecret(received, secret.Value))
        {
            return Unauthorized();
        }

        var outcome = await reminders.SendDailyAsync(clock.GetUtcNow(), OwnerClock.Zone());
        var body = new ReminderResponse(outcome.Result.ToString().ToLowerInvariant(), outcome.Items);

        // A failed send must end the scheduled flow in error, so GitHub warns the owner (spec 005, RNF-5).
        return outcome.Result == ReminderResult.Failed ? StatusCode(StatusCodes.Status502BadGateway, body) : Ok(body);
    }
}
