using EventManagement.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Controllers;

[ApiController]
[Route("api/facilities")]
[AllowAnonymous]
public class FacilitiesController : ControllerBase
{
    private readonly IEventService _events;
    public FacilitiesController(IEventService events) => _events = events;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _events.GetFacilitiesAsync());
}