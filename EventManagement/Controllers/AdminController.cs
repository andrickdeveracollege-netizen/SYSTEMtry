using EventManagement.Model.Domain;
using EventManagement.Model.DTO;
using EventManagement.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IEventService _events;
    public AdminController(IEventService events) => _events = events;

    private DepartmentType Dept =>
        Enum.TryParse<DepartmentType>(User.FindFirst("dept")?.Value, out var d)
            ? d : throw new ServiceException("Your admin account has no office assigned.", 403);

    private string Email => User.FindFirst("email")!.Value;

    [HttpGet("requests")]
    public async Task<IActionResult> Requests([FromQuery] EventStatus status = EventStatus.Pending)
        => Ok(await _events.GetRequestsForAdminAsync(Dept, status));

    [HttpPost("requests/{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        await _events.ApproveAsync(id, Dept, Email);
        return Ok(new { message = "Request approved." });
    }

    [HttpPost("requests/{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, RejectDto dto)
    {
        await _events.RejectAsync(id, Dept, Email, dto.Reason);
        return Ok(new { message = "Request rejected." });
    }

    [HttpGet("requests/{id:int}/endorsement")]
    public async Task<IActionResult> Endorsement(int id)
    {
        var (path, contentType, name) = await _events.GetEndorsementAsync(id, Dept);
        Response.Headers["X-Download-Name"] = Uri.EscapeDataString(name);
        return PhysicalFile(path, contentType);
    }
}