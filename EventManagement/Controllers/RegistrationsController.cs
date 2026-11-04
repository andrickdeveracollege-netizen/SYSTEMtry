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

    [HttpPost]
    public async Task<IActionResult> Register(RegistrationDto dto)
    {
        await _service.RegisterAsync(dto);
        return Ok(new { message = "You are registered. See you at the event!" });
    }
}