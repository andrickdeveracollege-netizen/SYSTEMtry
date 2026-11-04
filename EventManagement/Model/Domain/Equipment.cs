namespace EventManagement.Model.Domain;

public class Equipment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DepartmentType Department { get; set; }   // office that owns/approves it
    public bool IsOther { get; set; }                // "Others" needs a description
}