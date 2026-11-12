namespace EventManagement.Model.Domain;

public class EventEquipment
{
    public int Id { get; set; }

    public int DepartmentRequestId { get; set; }
    public DepartmentRequest? DepartmentRequest { get; set; }

    public int EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }
}
