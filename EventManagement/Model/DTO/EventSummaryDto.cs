using EventManagement.Model.Domain;

namespace EventManagement.Model.DTO;

public record EventSummaryDto(
    int Id,
    string Title,
    string Organization,
    string Purpose,
    List<int> FacilityIds,
    List<string> FacilityNames,
    DateTime StartTime,
    DateTime EndTime,
    YearLevel TargetAudience,
    int Capacity,
    int Registered,
    EventStatus Status);
