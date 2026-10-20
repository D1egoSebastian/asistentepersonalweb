using AsistentePersonal.DTOs;
using AsistentePersonal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AsistentePersonal.Controllers;

// Public endpoints: they work without a session, because they are how a session starts.
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpGet("status")]
    public async Task<AuthStatusResponse> Status() => new(await auth.HasOwnerAsync());

    [HttpPost("setup")]
    public async Task<IActionResult> Setup([FromBody] SetupRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        var result = await auth.SetupAsync(request.Email, request.Password, request.PasswordConfirmation, now);
        return ToSessionResult(result, now);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        var result = await auth.LoginAsync(request.Email, request.Password, now);
        return ToSessionResult(result, now);
    }

    // Ok → 200 with the session, Invalid → 400, Conflict → 409, any other failure → 401.
    private IActionResult ToSessionResult(ServiceResult<string> result, DateTimeOffset now) => result.Status switch
    {
        ResultStatus.Ok => Ok(new SessionResponse(result.Value!, now.Add(SessionTokens.Lifetime))),
        ResultStatus.Invalid => BadRequest(new { message = result.Message }),
        ResultStatus.Conflict => Conflict(new { message = result.Message }),
        _ => Unauthorized(new { message = result.Message }),
    };
}
