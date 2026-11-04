using EventManagement.Model.Domain;

namespace EventManagement.Model.DTO;

public record AdminRequestDto(
    int Id,
    string Title,
    string RequesterName,
    string RequesterEmail,
    string Organization,
    RequesterRole Role,
    YearLevel TargetAudience,
    string Purpose,
    string FacilityName,
    DateTime StartTime,
    DateTime EndTime,
    EventStatus Status,
    bool HasEndorsement,
    bool EndorsementOptional,
    string? EndorsementFileName,
    string? RejectionReason,
    DateTime CreatedAt);