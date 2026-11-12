using EventManagement.Model.Domain;

namespace EventManagement.Model.DTO;

public record MyRegistrationDto(
    int Id,
    int EventId,
    string EventTitle,
    DateTime StartTime,
    YearLevel YearLevel,
    bool Cancelled);
