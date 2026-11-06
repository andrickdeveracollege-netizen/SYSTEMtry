namespace EventManagement.Model.Domain;

public class EventRegistration
{
    public int Id { get; set; }
    public int CampusEventId { get; set; }
    public CampusEvent? CampusEvent { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public YearLevel YearLevel { get; set; }
    public bool Confirmed { get; set; }
    public bool Cancelled { get; set; }         // soft cancel, keeps history
    public DateTime RegisteredAt { get; set; } = DateTime.Now;
}