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
    public int FacilityId { get; set; }
    [StringLength(100)] public string? OtherFacilityName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public IFormFile? Endorsement { get; set; }
    public List<EquipmentRequestItemDto> Equipment { get; set; } = new();
}