using System.ComponentModel.DataAnnotations;

namespace EventManagement.Model.DTO;

public class RegisterAccountDto
{
    [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = "";
    [Required, StringLength(150)] public string FullName { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = "";
}