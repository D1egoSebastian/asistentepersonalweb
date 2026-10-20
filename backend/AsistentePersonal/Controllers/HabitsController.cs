using System.Security.Claims;
using AsistentePersonal.DTOs;
using AsistentePersonal.Models;
using AsistentePersonal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsistentePersonal.Controllers;

// Every endpoint needs a signed-in owner. The owner comes from the token, never from the request.
[ApiController]
[Route("api/habits")]
[Authorize]
public class HabitsController(HabitService habits) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var list = await habits.ListAsync(ownerId, OwnerClock.Today());
        return Ok(new HabitListResponse(list.Today, list.Habits.Select(ToResponse).ToList()));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHabitRequest request)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var result = await habits.CreateAsync(
            ownerId, request.Name, request.Description, request.Frequency, request.WeeklyGoal, OwnerClock.Today());
        if (result.Status != ResultStatus.Ok)
        {
            return ToActionResult(result.Status, result.Message);
        }

        return Ok(ToResponse(new HabitWithLogs(result.Value!, Array.Empty<DateOnly>())));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateHabitRequest request)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var result = await habits.UpdateAsync(ownerId, id, request.Name, request.Description);
        return ToActionResult(result.Status, result.Message);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var result = await habits.DeleteAsync(ownerId, id);
        return ToActionResult(result.Status, result.Message);
    }

    [HttpPost("{id:int}/log")]
    public async Task<IActionResult> Log(int id, [FromBody] LogRequest request)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var result = await habits.LogAsync(ownerId, id, request.Date, OwnerClock.Today());
        return ToActionResult(result.Status, result.Message);
    }

    [HttpDelete("{id:int}/log/{date}")]
    public async Task<IActionResult> Unlog(int id, string date)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var day))
        {
            return BadRequest(new { message = "La fecha debe tener el formato yyyy-MM-dd." });
        }

        var result = await habits.UnlogAsync(ownerId, id, day, OwnerClock.Today());
        return ToActionResult(result.Status, result.Message);
    }

    // Maps the service outcome to HTTP (plan §5.3): Ok → 200, Invalid → 400, NotFound → 404,
    // Conflict → 409, Unauthorized → 401.
    private IActionResult ToActionResult(ResultStatus status, string? message) => status switch
    {
        ResultStatus.Ok => Ok(),
        ResultStatus.Invalid => BadRequest(new { message }),
        ResultStatus.Conflict => Conflict(new { message }),
        ResultStatus.Unauthorized => Unauthorized(new { message }),
        _ => NotFound(new { message }),
    };

    private static HabitResponse ToResponse(HabitWithLogs item) => new(
        item.Habit.Id,
        item.Habit.Name,
        item.Habit.Description,
        item.Habit.Frequency,
        item.Habit.WeeklyGoal,
        item.Habit.CreatedOn,
        item.LogDates);

    private bool TryGetOwnerId(out int ownerId) =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out ownerId);
}
