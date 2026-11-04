using System.ComponentModel.DataAnnotations;
using EventManagement.Model.Domain;

namespace EventManagement.Model.DTO;

public class RegistrationDto
{
    public int EventId { get; set; }
    [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = "";
    [Required, StringLength(150)] public string FullName { get; set; } = "";
    public YearLevel YearLevel { get; set; }
    public bool Confirmed { get; set; }
}