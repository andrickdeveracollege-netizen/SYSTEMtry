namespace EventManagement.Model.Domain;

public class EventEquipment
{
    public int Id { get; set; }

    public int CampusEventId { get; set; }
    public CampusEvent? CampusEvent { get; set; }

    public int EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }

    public EventStatus Status { get; set; } = EventStatus.Pending;
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
}