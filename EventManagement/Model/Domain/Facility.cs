namespace EventManagement.Model.Domain;

public class Facility
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DepartmentType Department { get; set; }
    public ICollection<CampusEvent> Events { get; set; } = new List<CampusEvent>();
}