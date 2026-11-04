using EventManagement.Model.Domain;
using EventManagement.Model.DTO;

namespace EventManagement.Mapper;

public static class EventEquipmentMapper
{
    public static AdminEquipmentRequestDto ToAdminDto(this EventEquipment x) => new(
        x.Id,
        x.CampusEventId,
        x.CampusEvent?.Title ?? "",
        x.CampusEvent?.RequesterName ?? "",
        x.CampusEvent?.RequesterEmail ?? "",
        x.CampusEvent?.Organization ?? "",
        x.CampusEvent?.Facility?.Name ?? "",
        x.CampusEvent?.StartTime ?? default,
        x.CampusEvent?.EndTime ?? default,
        x.Equipment?.Name ?? "",
        x.Equipment?.IsOther ?? false,
        x.Quantity,
        x.Notes,
        x.Status,
        x.RejectionReason);
}