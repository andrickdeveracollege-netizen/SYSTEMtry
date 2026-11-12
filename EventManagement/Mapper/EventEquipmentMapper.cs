using EventManagement.Model.Domain;
using EventManagement.Model.DTO;

namespace EventManagement.Mapper;

public static class EventEquipmentMapper
{
    public static AdminEquipmentRequestDto ToAdminDto(this EventEquipment x) => new(
        x.Id,
        x.DepartmentRequest?.CampusEventId ?? 0,
        x.DepartmentRequest?.CampusEvent?.Title ?? "",
        x.DepartmentRequest?.CampusEvent?.RequesterName ?? "",
        x.DepartmentRequest?.CampusEvent?.RequesterEmail ?? "",
        x.DepartmentRequest?.CampusEvent?.Organization ?? "",
        x.DepartmentRequest?.Facilities.FirstOrDefault()?.OtherName
            ?? x.DepartmentRequest?.Facilities.FirstOrDefault()?.Facility?.Name ?? "",
        x.DepartmentRequest?.CampusEvent?.StartTime ?? default,
        x.DepartmentRequest?.CampusEvent?.EndTime ?? default,
        x.Equipment?.Name ?? "",
        x.Equipment?.IsOther ?? false,
        x.Quantity,
        x.Notes,
        x.DepartmentRequest?.Status ?? EventStatus.Pending,
        x.DepartmentRequest?.RejectionReason);
}