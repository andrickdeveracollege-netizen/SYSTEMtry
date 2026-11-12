namespace EventManagement.Model.Domain;

public class EventFacility
{
    public int Id { get; set; }

    public int DepartmentRequestId { get; set; }
    public DepartmentRequest? DepartmentRequest { get; set; }

    public int FacilityId { get; set; }
    public Facility? Facility { get; set; }

    public string? OtherName { get; set; }    // only when the facility is "Others"
}
