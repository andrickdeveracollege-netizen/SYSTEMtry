using System.ComponentModel.DataAnnotations;
using EventManagement.Model.Domain;

namespace EventManagement.Model.DTO;

// Posted directly by the super admin. Approved right away, no registration needed.
public class InstitutionalEventDto
{
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [Required, StringLength(150)] public string Organization { get; set; } = "";
    [Required, StringLength(2000)] public string Purpose { get; set; } = "";
    public YearLevel TargetAudience { get; set; } = YearLevel.All;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    [Required, MinLength(1)] public List<int> FacilityIds { get; set; } = new();
}
