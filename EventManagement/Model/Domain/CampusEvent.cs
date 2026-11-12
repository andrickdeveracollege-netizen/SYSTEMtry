namespace EventManagement.Model.Domain;

public class CampusEvent
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    public string RequesterName { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;
    public string? RequesterPhone { get; set; }               // guests only
    public string Organization { get; set; } = string.Empty;   // Department / Org / Club
    public RequesterRole Role { get; set; }
    public YearLevel TargetAudience { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public int Capacity { get; set; }                          // expected attendees = max registrations

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public bool IsGuest { get; set; }                          // requested by an outsider
    public bool IsInstitutional { get; set; }                  // posted by an administrator / super admin
    public bool RequiresRegistration { get; set; } = true;     // false for institutional events
    public string ManageCode { get; set; } = string.Empty;     // lets a guest manage the request without an account

    // Overall status, derived from the department requests (see EventService.RecomputeStatus)
    public EventStatus Status { get; set; } = EventStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<DepartmentRequest> DepartmentRequests { get; set; } = new List<DepartmentRequest>();
    public ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
}
