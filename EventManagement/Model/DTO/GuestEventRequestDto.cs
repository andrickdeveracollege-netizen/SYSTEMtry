using System.ComponentModel.DataAnnotations;

namespace EventManagement.Model.DTO;

// Outsiders request without an account, but they must give an email.
public class GuestEventRequestDto : EventRequestDto
{
    [Required, EmailAddress, StringLength(200)] public string GuestEmail { get; set; } = "";
    [StringLength(30)] public string? Phone { get; set; }
}
