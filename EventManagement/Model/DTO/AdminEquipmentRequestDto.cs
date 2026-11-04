using EventManagement.Model.Domain;

namespace EventManagement.Model.DTO;

public record AdminEquipmentRequestDto(
    int Id,
    int EventId,
    string EventTitle,
    string RequesterName,
    string RequesterEmail,
    string Organization,
    string FacilityName,
    DateTime StartTime,
    DateTime EndTime,
    string EquipmentName,
    bool IsOther,
    int Quantity,
    string? Notes,
    EventStatus Status,
    string? RejectionReason);