using EventManagement.Model.Domain;

namespace EventManagement.Model.DTO;

public record EventDetailDto(
    EventSummaryDto Event,
    string RequesterName,
    RequesterRole Role,
    List<DepartmentStatusDto> Departments);
