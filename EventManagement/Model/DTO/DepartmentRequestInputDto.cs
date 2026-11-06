using EventManagement.Model.Domain;

namespace EventManagement.Model.DTO;

// One ticked department in the "Request an Event" form.
public class DepartmentRequestInputDto
{
    public DepartmentType Department { get; set; }
    public List<int> FacilityIds { get; set; } = new();
    public List<EquipmentRequestItemDto> Equipment { get; set; } = new();
    public IFormFile? Endorsement { get; set; }   // this department's letter
}
