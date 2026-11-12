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

    // Public: shared facility calendar (approved + pending, so slots stay held while under review)
    [HttpGet("calendar")]
    [AllowAnonymous]
    public async Task<IActionResult> Calendar([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int? facilityId)
        => Ok(await _events.GetCalendarAsync(from, to, facilityId));

    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> Mine() => Ok(await _events.GetMineAsync(User.Email()));

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Detail(int id) => Ok(await _events.GetDetailAsync(id));

    // The requester's role comes from the account (email format), it is not typed in the form
    [HttpPost("request")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(40_000_000)]
    public async Task<IActionResult> SubmitRequest([FromForm] EventRequestDto dto)
    {
        User.EnsureNotAdmin("request events. The super admin posts institutional events instead");
        var who = new Requester(User.Email(), User.Position(), IsGuest: false);
        var created = await _events.CreateRequestAsync(dto, who);
        return CreatedAtAction(nameof(Detail), new { id = created.Event.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, EventUpdateDto dto)
        => Ok(await _events.UpdateAsync(id, dto, User.Email()));

    [HttpPut("{id:int}/reschedule")]
    [Authorize]
    public async Task<IActionResult> Reschedule(int id, RescheduleDto dto)
        => Ok(await _events.RescheduleAsync(id, dto, User.Email()));

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Cancel(int id)
    {
        await _events.CancelAsync(id, User.Email());
        return NoContent();
    }
}
