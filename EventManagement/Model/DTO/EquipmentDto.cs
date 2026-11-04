using EventManagement.Model.Domain;

namespace EventManagement.Model.DTO;

public record EquipmentDto(
    int Id,
    string Name,
    DepartmentType Department,
    string ApprovingOffice,
    bool IsOther);