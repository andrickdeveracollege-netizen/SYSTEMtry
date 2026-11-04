using EventManagement.Model.Domain;

namespace EventManagement.Model.DTO;

public record EventSummaryDto(
    int Id,
    string Title,
    string Organization,
    string Purpose,
    int FacilityId,
    string FacilityName,
    DateTime StartTime,
    DateTime EndTime,
    YearLevel TargetAudience,
    EventStatus Status);