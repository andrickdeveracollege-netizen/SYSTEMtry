using EventManagement.Model.Domain;
using EventManagement.Model.DTO;

namespace EventManagement.Mapper;

public static class EventMapper
{
    public static EventSummaryDto ToSummary(this CampusEvent e) => new(
        e.Id, e.Title, e.Organization, e.Purpose,
        e.FacilityId, e.Facility?.Name ?? "", e.StartTime, e.EndTime,
        e.TargetAudience, e.Status);

    public static AdminRequestDto ToAdminDto(this CampusEvent e) => new(
        e.Id, e.Title, e.RequesterName, e.RequesterEmail, e.Organization,
        e.Role, e.TargetAudience, e.Purpose,
        e.Facility?.Name ?? "", e.StartTime, e.EndTime,
        e.Status, e.EndorsementStoredName != null,
        e.ApproverDepartment == DepartmentType.College,
        e.EndorsementOriginalName, e.RejectionReason, e.CreatedAt);
}