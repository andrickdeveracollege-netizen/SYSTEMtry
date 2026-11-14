using EventManagement.Model.DTO;
using EventManagement.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Controllers;

[ApiController]
[Route("api/registrations")]
[Authorize]
public class RegistrationsController : ControllerBase
{
    private readonly IRegistrationService _service;
    public RegistrationsController(IRegistrationService service) => _service = service;

    [HttpGet("mine")]
    public async Task<IActionResult> Mine() => Ok(await _service.GetMineAsync(User.Email()));

    [HttpPost]
    public async Task<IActionResult> Register(RegistrationDto dto)
    {
        User.EnsureNotAdmin("register for events");
        var who = new Registrant(User.Email(), User.FullName(), User.Position(), User.Level());
        await _service.RegisterMemberAsync(dto.EventId, dto.Confirmed, who);
        return StatusCode(StatusCodes.Status201Created, new { message = "You are registered. See you at the event!" });
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        await _service.CancelAsync(id, User.Email());
        return Ok(new { message = "Registration cancelled." });
    }
}
