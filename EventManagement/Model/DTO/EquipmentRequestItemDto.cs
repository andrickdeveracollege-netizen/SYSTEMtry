using System.ComponentModel.DataAnnotations;

namespace EventManagement.Model.DTO;

public class EquipmentRequestItemDto
{
    public int EquipmentId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; } = 1;

    [StringLength(500)]
    public string? Notes { get; set; }
}