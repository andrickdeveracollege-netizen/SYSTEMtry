using EventManagement.Model.Domain;

namespace EventManagement.Model.DTO;

// One department's slice of an event, as seen by that department's admin.
// Id = DepartmentRequest.Id
public record AdminRequestDto(
    int Id,
    int EventId,
    string Title,
    string RequesterName,
    string RequesterEmail,
    string Organization,
    RequesterRole Role,
    YearLevel TargetAudience,
    string? AudienceDetail,
    string Purpose,
    DateTime StartTime,
    DateTime EndTime,
    List<string> Facilities,
    List<AdminEquipmentLineDto> Equipment,
    EventStatus Status,
    bool HasEndorsement,
    bool EndorsementOptional,
    string? EndorsementFileName,
    string? RejectionReason,
    string? Note,
    DateTime CreatedAt);
