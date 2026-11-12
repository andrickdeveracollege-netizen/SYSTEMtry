using EventManagement.Model.Domain;
using EventManagement.Model.DTO;

namespace EventManagement.Mapper;

public static class EventMapper
{
    public static EventSummaryDto ToSummary(this CampusEvent e)
    {
        var facility = e.DepartmentRequests.SelectMany(r => r.Facilities).FirstOrDefault();
        return new EventSummaryDto(
            e.Id, e.Title, e.Organization, e.Purpose,
            facility?.FacilityId ?? 0, FacilityName(facility), e.StartTime, e.EndTime,
            e.TargetAudience, e.Status);
    }

    public static AdminRequestDto ToAdminDto(this CampusEvent e, DepartmentType department)
    {
        var request = e.DepartmentRequests.FirstOrDefault(r => r.Department == department);
        var facility = request?.Facilities.FirstOrDefault();
        return new AdminRequestDto(
            e.Id, e.Title, e.RequesterName, e.RequesterEmail, e.Organization,
            e.Role, e.TargetAudience, e.Purpose,
            FacilityName(facility), e.StartTime, e.EndTime,
            request?.Status ?? e.Status, request?.EndorsementStoredName != null,
            department == DepartmentType.College,
            request?.EndorsementOriginalName, request?.RejectionReason, e.CreatedAt);
    }

    private static string FacilityName(EventFacility? facility) =>
        facility?.OtherName ?? facility?.Facility?.Name ?? "";
}