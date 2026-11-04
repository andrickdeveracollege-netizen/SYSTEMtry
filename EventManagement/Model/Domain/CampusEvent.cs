namespace EventManagement.Model.Domain;

public class CampusEvent
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    public string RequesterName { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;   // Department / Org / Club
    public RequesterRole Role { get; set; }
    public YearLevel TargetAudience { get; set; }
    public string Purpose { get; set; } = string.Empty;

    public int FacilityId { get; set; }
    public Facility? Facility { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public EventStatus Status { get; set; } = EventStatus.Pending;
    public DepartmentType ApproverDepartment { get; set; }

    public string? EndorsementStoredName { get; set; }
    public string? EndorsementOriginalName { get; set; }

    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
    public ICollection<EventEquipment> EquipmentItems { get; set; } = new List<EventEquipment>();
}