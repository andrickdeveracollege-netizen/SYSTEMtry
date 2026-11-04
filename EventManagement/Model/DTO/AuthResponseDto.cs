using EventManagement.Model.Domain;

namespace EventManagement.Model.DTO;

public record AuthResponseDto(
    string Token,
    string Email,
    string FullName,
    bool IsAdmin,
    DepartmentType? Department);