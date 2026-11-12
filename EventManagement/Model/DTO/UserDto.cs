using EventManagement.Model.Domain;

namespace EventManagement.Model.DTO;

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    UserRole Role,
    RequesterRole Position,
    YearLevel Level,
    DepartmentType? Department);
