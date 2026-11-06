using EventManagement.Model.Domain;

namespace EventManagement.Model.DTO;

public record EventReportRowDto(
    int EventId,
    string Title,
    DateTime StartTime,
    EventStatus EventStatus,
    EventStatus DepartmentStatus,
    int Capacity,
    int Registered,
    int Remaining);
