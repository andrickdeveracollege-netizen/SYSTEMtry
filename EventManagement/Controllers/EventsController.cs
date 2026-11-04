using EventManagement.Model.DTO;
using EventManagement.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _events;
    public EventsController(IEventService events) => _events = events;

    // Public: bulletin board on the landing page (approved, upcoming)
    [HttpGet("bulletin")]
    [AllowAnonymous]
    public async Task<IActionResult> Bulletin() => Ok(await _events.GetBulletinAsync());

    // Shared facility calendar (approved + pending, so slots are held while under review)
    [HttpGet("calendar")]
    [Authorize]
    public async Task<IActionResult> Calendar([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int? facilityId)
        => Ok(await _events.GetCalendarAsync(from, to, facilityId));

    [HttpPost("request")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6_000_000)]
    public async Task<IActionResult> SubmitRequest([FromForm] EventRequestDto dto)
    {
        var email = User.FindFirst("email")!.Value;
        return Ok(await _events.CreateRequestAsync(dto, email));
    }
}