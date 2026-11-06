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

    private string Email => User.FindFirst("email")!.Value;

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
    public async Task<IActionResult> Mine() => Ok(await _events.GetMineAsync(Email));

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Detail(int id) => Ok(await _events.GetDetailAsync(id));

    [HttpPost("request")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> SubmitRequest([FromForm] EventRequestDto dto)
    {
        var created = await _events.CreateRequestAsync(dto, Email);
        return CreatedAtAction(nameof(Detail), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, EventUpdateDto dto)
        => Ok(await _events.UpdateAsync(id, dto, Email));

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Cancel(int id)
    {
        await _events.CancelAsync(id, Email);
        return NoContent();
    }
}
