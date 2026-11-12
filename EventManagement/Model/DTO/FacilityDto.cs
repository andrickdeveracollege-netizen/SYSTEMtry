using EventManagement.Model.Domain;

namespace EventManagement.Model.DTO;

public record FacilityDto(
    int Id,
    string Name,
    DepartmentType Department,
    string ApprovingOffice,
    bool EndorsementOptional,
    int? Capacity,
    bool IsOther);
