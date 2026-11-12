namespace EventManagement.Model.Domain;

public class Facility
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DepartmentType Department { get; set; }
    public int? Capacity { get; set; }        // max people, null = no limit
    public bool IsOther { get; set; }         // "Others": the requester types the facility name
    public ICollection<EventFacility> EventFacilities { get; set; } = new List<EventFacility>();
}
