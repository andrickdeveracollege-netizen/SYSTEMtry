namespace EventManagement.Model.DTO;

// Who registered for an event. Only the requester (and the offices) can see it.
public record RegistrantListDto(int EventId, string Title, DateTime StartTime, int Capacity, List<RegistrantRowDto> Rows);
