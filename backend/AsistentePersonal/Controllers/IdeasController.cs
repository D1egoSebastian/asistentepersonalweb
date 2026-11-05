using System.Security.Claims;
using AsistentePersonal.DTOs;
using AsistentePersonal.Models;
using AsistentePersonal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsistentePersonal.Controllers;

// Every endpoint needs a signed-in owner. The owner comes from the token, never from the request.
[ApiController]
[Route("api/ideas")]
[Authorize]
public class IdeasController(IdeaService ideas) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool archived = false, [FromQuery] string? search = null, [FromQuery] string? tag = null)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var list = await ideas.ListAsync(ownerId, archived, search, tag, OwnerClock.Zone());
        return Ok(new IdeaListResponse(list.Select(ToSummary).ToList()));
    }

    // Declared before the {id} routes for readability; the {id:int} constraint keeps "tags" from matching them.
    [HttpGet("tags")]
    public async Task<IActionResult> Tags()
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var counts = await ideas.TagCountsAsync(ownerId);
        return Ok(counts.Select(c => new TagCountResponse(c.Name, c.ActiveCount, c.ArchivedCount)).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        return ToActionResult(await ideas.GetAsync(ownerId, id), ToResponse);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] IdeaRequest request)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var result = await ideas.CreateAsync(ownerId, request.Text, request.Tags, DateTimeOffset.UtcNow);
        return ToActionResult(result, ToResponse);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] IdeaRequest request)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        if (request.Version is null)
        {
            return BadRequest(new { message = "Falta la versión de la idea." });
        }

        var result = await ideas.UpdateAsync(
            ownerId, id, request.Text, request.Tags, request.Version.Value, DateTimeOffset.UtcNow);
        return ToActionResult(result, ToResponse);
    }

    [HttpPost("{id:int}/archive")]
    public async Task<IActionResult> Archive(int id)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        return ToActionResult(await ideas.ArchiveAsync(ownerId, id, DateTimeOffset.UtcNow));
    }

    [HttpPost("{id:int}/restore")]
    public async Task<IActionResult> Restore(int id)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        return ToActionResult(await ideas.RestoreAsync(ownerId, id, DateTimeOffset.UtcNow));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        return ToActionResult(await ideas.DeleteAsync(ownerId, id));
    }

    // Maps the service outcome to HTTP (plan §5.3): Ok → 200, Invalid → 400, NotFound → 404,
    // Conflict → 409, Unauthorized → 401. Every error carries the message in Spanish.
    private IActionResult ToActionResult<T>(ServiceResult<T> result, Func<T, object> map) =>
        result.Status == ResultStatus.Ok
            ? Ok(map(result.Value!))
            : ErrorResult(result.Status, result.Message);

    private IActionResult ToActionResult(ServiceResult result) =>
        result.Status == ResultStatus.Ok ? Ok() : ErrorResult(result.Status, result.Message);

    private IActionResult ErrorResult(ResultStatus status, string? message) => status switch
    {
        ResultStatus.Invalid => BadRequest(new { message }),
        ResultStatus.Conflict => Conflict(new { message }),
        ResultStatus.Unauthorized => Unauthorized(new { message }),
        _ => NotFound(new { message }),
    };

    private static IdeaSummaryResponse ToSummary(IdeaListItem item) =>
        new(item.Id, item.Summary, item.Tags, item.CreatedDate, item.UpdatedDate);

    private static IdeaResponse ToResponse(Idea idea) => new(
        idea.Id,
        idea.Text,
        idea.Tags,
        OwnerClock.TodayAt(idea.CreatedAt, OwnerClock.Zone()),
        idea.ArchivedAt is not null,
        idea.Version);

    private bool TryGetOwnerId(out int ownerId) =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out ownerId);
}
