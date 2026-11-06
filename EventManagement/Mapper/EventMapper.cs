using EventManagement.Model.Domain;
using EventManagement.Model.DTO;

namespace EventManagement.Mapper;

public static class EventMapper
{
    // Needs DepartmentRequests -> Facilities -> Facility and Registrations loaded
    public static EventSummaryDto ToSummary(this CampusEvent e)
    {
        var facilities = e.DepartmentRequests
            .SelectMany(d => d.Facilities)
            .Where(f => f.Facility != null)
            .Select(f => f.Facility!)
            .DistinctBy(f => f.Id)
            .ToList();

        return new(
            e.Id, e.Title, e.Organization, e.Purpose,
            facilities.Select(f => f.Id).ToList(),
            facilities.Select(f => f.Name).ToList(),
            e.StartTime, e.EndTime, e.TargetAudience,
            e.Capacity, e.Registrations.Count(r => !r.Cancelled), e.Status);
    }

    public static EventDetailDto ToDetail(this CampusEvent e) => new(
        e.ToSummary(), e.RequesterName, e.Role,
        e.DepartmentRequests
            .Select(d => new DepartmentStatusDto(FacilityMapper.OfficeName(d.Department), d.Status, d.RejectionReason))
            .ToList());
}
