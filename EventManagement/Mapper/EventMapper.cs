using EventManagement.Model.Domain;
using EventManagement.Model.DTO;

namespace EventManagement.Mapper;

public static class EventMapper
{
    // Needs DepartmentRequests -> Facilities -> Facility and Registrations loaded
    public static EventSummaryDto ToSummary(this CampusEvent e)
    {
        var rows = e.DepartmentRequests.SelectMany(d => d.Facilities).Where(f => f.Facility != null).ToList();

        return new(
            e.Id, e.Title, e.Organization, e.Purpose,
            rows.Select(f => f.FacilityId).Distinct().ToList(),
            rows.Select(f => f.DisplayName()).Distinct().ToList(),
            e.StartTime, e.EndTime, e.TargetAudience, e.AudienceDetail,
            e.Capacity, e.Registrations.Count(r => !r.Cancelled),
            e.IsInstitutional, e.RequiresRegistration, e.Status);
    }

    public static EventDetailDto ToDetail(this CampusEvent e) => new(
        e.ToSummary(), e.RequesterName, e.Role,
        e.DepartmentRequests
            .Select(d => new DepartmentStatusDto(FacilityMapper.OfficeName(d.Department), d.Status, d.RejectionReason, d.AdminNote))
            .ToList());
}
