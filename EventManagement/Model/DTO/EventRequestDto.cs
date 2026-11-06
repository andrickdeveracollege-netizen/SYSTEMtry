using System.ComponentModel.DataAnnotations;
using EventManagement.Model.Domain;

namespace EventManagement.Model.DTO;

public class EventRequestDto
{
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [Required, StringLength(150)] public string RequesterName { get; set; } = "";
    [Required, StringLength(150)] public string Organization { get; set; } = "";
    public RequesterRole Role { get; set; }
    public YearLevel TargetAudience { get; set; }
    [Required, StringLength(2000)] public string Purpose { get; set; } = "";
    [Range(1, 10000)] public int Capacity { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    // Only the departments the requester ticked
    public List<DepartmentRequestInputDto> Departments { get; set; } = new();
}
