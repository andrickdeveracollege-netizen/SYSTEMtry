using EventManagement.Model.Domain;
using EventManagement.Model.DTO;
using EventManagement.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Controllers;

[ApiController]
[Route("api/admin/equipment-requests")]
[Authorize(Roles = "Admin")]
public class AdminEquipmentController : ControllerBase
{
    private readonly IEquipmentService _equipment;
    public AdminEquipmentController(IEquipmentService equipment) => _equipment = equipment;

    private DepartmentType Dept =>
        Enum.TryParse<DepartmentType>(User.FindFirst("dept")?.Value, out var d)
            ? d : throw new ServiceException("Your admin account has no office assigned.", 403);

    private string Email => User.FindFirst("email")!.Value;

    [HttpGet]
    public async Task<IActionResult> Requests([FromQuery] EventStatus status = EventStatus.Pending)
        => Ok(await _equipment.GetRequestsForAdminAsync(Dept, status));

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        await _equipment.ApproveAsync(id, Dept, Email);
        return Ok(new { message = "Equipment request approved." });
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, RejectDto dto)
    {
        await _equipment.RejectAsync(id, Dept, Email, dto.Reason);
        return Ok(new { message = "Equipment request rejected." });
    }
}