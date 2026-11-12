using EventManagement.Model.DTO;
using EventManagement.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Controllers;

// admin@ustangelicum.edu.ph: resets passwords, sets positions, posts institutional events
[ApiController]
[Route("api/superadmin")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : ControllerBase
{
    private readonly IUserAdminService _users;
    private readonly IEventService _events;

    public SuperAdminController(IUserAdminService users, IEventService events)
    {
        _users = users;
        _events = events;
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users() => Ok(await _users.GetUsersAsync());

    [HttpPost("users/{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id)
    {
        await _users.ResetPasswordAsync(id);
        return Ok(new { message = "Password reset to the default. Ask the user to change it after signing in." });
    }

    [HttpPut("users/{id:guid}/position")]
    public async Task<IActionResult> SetPosition(Guid id, SetPositionDto dto)
    {
        await _users.SetPositionAsync(id, dto);
        return Ok(new { message = "Position updated." });
    }

    [HttpPost("events")]
    public async Task<IActionResult> PostInstitutional(InstitutionalEventDto dto)
    {
        var created = await _events.CreateInstitutionalAsync(dto, User.Email(), User.FullName());
        return Ok(created);
    }
}
