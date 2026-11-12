using EventManagement.Model.Domain;
using EventManagement.Model.DTO;
using EventManagement.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Controllers;

// Office admins act on their own office. The super admin can act on any office (pass ?dept=).
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController : ControllerBase
{
    private readonly IEventService _events;
    public AdminController(IEventService events) => _events = events;

    // null = super admin (no office restriction)
    private DepartmentType? Scope => User.IsInRole("SuperAdmin") ? null : User.Dept()
        ?? throw new ServiceException("Your admin account has no office assigned.", 403);

    // For lists: which office's requests to show
    private DepartmentType Office(DepartmentType? dept) =>
        Scope ?? dept ?? throw new ServiceException("Choose an office.");

    private string Email => User.Email();

    [HttpGet("requests")]
    public async Task<IActionResult> Requests([FromQuery] EventStatus status = EventStatus.Pending, [FromQuery] DepartmentType? dept = null)
        => Ok(await _events.GetRequestsForAdminAsync(Office(dept), status));

    [HttpGet("reports/events")]
    public async Task<IActionResult> Report([FromQuery] DepartmentType? dept = null)
        => Ok(await _events.GetReportAsync(Office(dept)));

    [HttpPost("requests/{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        await _events.ApproveAsync(id, Scope, Email);
        return Ok(new { message = "Request approved." });
    }

    [HttpPost("requests/{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, RejectDto dto)
    {
        await _events.RejectAsync(id, Scope, Email, dto.Reason);
        return Ok(new { message = "Request rejected." });
    }

    [HttpGet("requests/{id:int}/endorsement")]
    public async Task<IActionResult> Endorsement(int id)
    {
        var (path, contentType, name) = await _events.GetEndorsementAsync(id, Scope);
        Response.Headers["X-Download-Name"] = Uri.EscapeDataString(name);
        return PhysicalFile(path, contentType);
    }

    // Notes for the requester. Allowed in every status (pending, approved, rejected).
    [HttpPut("requests/{id:int}/note")]
    public async Task<IActionResult> Note(int id, NoteDto dto)
    {
        await _events.SetNoteAsync(id, Scope, dto.Note);
        return Ok(new { message = "Note saved." });
    }

    // Admins can cancel (soft delete) an event
    [HttpDelete("events/{id:int}")]
    public async Task<IActionResult> CancelEvent(int id)
    {
        await _events.AdminCancelEventAsync(id, Scope);
        return NoContent();
    }
}
