using System.ComponentModel.DataAnnotations;

namespace EventManagement.Model.DTO;

// Year level is never typed: students get it from their email, guests are "Outsider".
public class RegistrationDto
{
    public int EventId { get; set; }
    [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = "";
    [Required, StringLength(150)] public string FullName { get; set; } = "";
    public bool Confirmed { get; set; }
}
