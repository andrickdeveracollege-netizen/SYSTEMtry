namespace EventManagement.Model.Domain;

// What ONE department (Auxiliary / ITMC / College / TVET) is being asked for in an event:
// the facilities, the equipment, and that department's own endorsement letter.
public class DepartmentRequest
{
    public int Id { get; set; }

    public int CampusEventId { get; set; }
    public CampusEvent? CampusEvent { get; set; }

    public DepartmentType Department { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Pending;

    public string? EndorsementStoredName { get; set; }
    public string? EndorsementOriginalName { get; set; }

    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? AdminNote { get; set; }       // office note, can be added or changed in any status
    public DateTime? NoteUpdatedAt { get; set; }

    public ICollection<EventFacility> Facilities { get; set; } = new List<EventFacility>();
    public ICollection<EventEquipment> EquipmentItems { get; set; } = new List<EventEquipment>();
}
