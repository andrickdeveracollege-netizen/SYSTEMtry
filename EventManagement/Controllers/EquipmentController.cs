using EventManagement.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Controllers;

[ApiController]
[Route("api/equipment")]
[Authorize]
public class EquipmentController : ControllerBase
{
    private readonly IEquipmentService _equipment;
    public EquipmentController(IEquipmentService equipment) => _equipment = equipment;

    [HttpGet]
    public async Task<IActionResult> GetCatalog() => Ok(await _equipment.GetCatalogAsync());
}