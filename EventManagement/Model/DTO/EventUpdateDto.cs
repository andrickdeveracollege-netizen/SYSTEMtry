using System.ComponentModel.DataAnnotations;

namespace EventManagement.Model.DTO;

public class EventUpdateDto
{
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [Required, StringLength(2000)] public string Purpose { get; set; } = "";
    [Range(1, 10000)] public int Capacity { get; set; }
}
