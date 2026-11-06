using EventManagement.Model.Domain;
using EventManagement.Model.DTO;

namespace EventManagement.Mapper;

public static class DepartmentRequestMapper
{
    // Needs CampusEvent, Facilities -> Facility, EquipmentItems -> Equipment loaded
    public static AdminRequestDto ToAdminDto(this DepartmentRequest d)
    {
        var e = d.CampusEvent!;
        return new(
            d.Id, e.Id, e.Title, e.RequesterName, e.RequesterEmail, e.Organization,
            e.Role, e.TargetAudience, e.Purpose, e.StartTime, e.EndTime,
            d.Facilities.Select(f => f.Facility!.Name).ToList(),
            d.EquipmentItems.Select(i => new AdminEquipmentLineDto(i.Equipment!.Name, i.Quantity, i.Notes)).ToList(),
            d.Status,
            d.EndorsementStoredName != null,
            d.Department == DepartmentType.College,
            d.EndorsementOriginalName,
            d.RejectionReason,
            e.CreatedAt);
    }
}
