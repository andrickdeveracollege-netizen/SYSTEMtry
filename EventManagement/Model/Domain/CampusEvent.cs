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
    public int Capacity { get; set; }          // max registrations (tickets)

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    // Overall status, derived from the department requests (see EventService.RecomputeStatus)
    public EventStatus Status { get; set; } = EventStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // One row per department the requester ticked
    public ICollection<DepartmentRequest> DepartmentRequests { get; set; } = new List<DepartmentRequest>();
    public ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
}
