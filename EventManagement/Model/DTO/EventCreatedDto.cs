namespace EventManagement.Model.DTO;

// ManageCode lets a guest cancel / move the request later (accounts do not need it).
public record EventCreatedDto(EventSummaryDto Event, string ManageCode);
