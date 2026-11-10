using EventManagement.Model.Domain;
using EventManagement.Model.DTO;
using EventManagement.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Controllers;

// For outsiders (no account). They give an email; weekends only.
[ApiController]
[Route("api/guest")]
[AllowAnonymous]
public class GuestController : ControllerBase
{
    private readonly IEventService _events;
    private readonly IRegistrationService _registrations;

    public GuestController(IEventService events, IRegistrationService registrations)
    {
        _events = events;
        _registrations = registrations;
    }

    [HttpPost("events/request")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(40_000_000)]
    public async Task<IActionResult> Request([FromForm] GuestEventRequestDto dto)
    {
        var who = new Requester(dto.GuestEmail, RequesterRole.Guest, IsGuest: true, dto.Phone);
        return Ok(await _events.CreateRequestAsync(dto, who));
    }

    // Manage with the email + code that was shown after submitting
    [HttpGet("events/{id:int}")]
    public async Task<IActionResult> Get(int id, [FromQuery] string email, [FromQuery] string code)
        => Ok(await _events.GetGuestEventAsync(id, email, code));

    [HttpPut("events/{id:int}/reschedule")]
    public async Task<IActionResult> Reschedule(int id, RescheduleDto dto, [FromQuery] string email, [FromQuery] string code)
        => Ok(await _events.RescheduleGuestAsync(id, dto, email, code));

    [HttpDelete("events/{id:int}")]
    public async Task<IActionResult> Cancel(int id, [FromQuery] string email, [FromQuery] string code)
    {
        await _events.CancelGuestAsync(id, email, code);
        return NoContent();
    }

    [HttpPost("registrations")]
    public async Task<IActionResult> Register(RegistrationDto dto)
    {
        await _registrations.RegisterGuestAsync(dto);
        return Ok(new { message = "You are registered. See you at the event!" });
    }
}
