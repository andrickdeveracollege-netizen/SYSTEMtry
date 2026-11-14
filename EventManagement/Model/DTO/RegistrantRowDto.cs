namespace EventManagement.Model.DTO;

public record RegistrantRowDto(string FullName, string Email, string Level, bool Confirmed, DateTime RegisteredAt);
