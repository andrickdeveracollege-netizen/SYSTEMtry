using EventManagement.Model.Domain;
using EventManagement.Model.DTO;
using EventManagement.Repository;

namespace EventManagement.Service;

public class RegistrationService : IRegistrationService
{
    private static readonly YearLevel[] SingleLevels =
        { YearLevel.Elementary, YearLevel.JHS, YearLevel.SHS, YearLevel.College };

    private readonly IEventRepository _events;
    private readonly IRegistrationRepository _registrations;

    public RegistrationService(IEventRepository events, IRegistrationRepository registrations)
    {
        _events = events;
        _registrations = registrations;
    }

    public async Task RegisterAsync(RegistrationDto dto)
    {
        if (!dto.Confirmed) throw new ServiceException("Please confirm your registration.");
        if (!SingleLevels.Contains(dto.YearLevel)) throw new ServiceException("Please select a valid year level.");

        var ev = await _events.GetByIdAsync(dto.EventId) ?? throw new ServiceException("Event not found.", 404);
        if (ev.Status != EventStatus.Approved) throw new ServiceException("This event is not open for registration.");
        if (ev.EndTime < DateTime.Now) throw new ServiceException("This event has already ended.");

        // Year-level must match the event's target audience
        if ((ev.TargetAudience & dto.YearLevel) == 0)
            throw new ServiceException($"This event is only open to: {ev.TargetAudience}. Your year level ({dto.YearLevel}) is not included.");

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _registrations.ExistsAsync(ev.Id, email))
            throw new ServiceException("You are already registered for this event.", 409);

        await _registrations.AddAsync(new EventRegistration
        {
            CampusEventId = ev.Id,
            Email = email,
            FullName = dto.FullName.Trim(),
            YearLevel = dto.YearLevel,
            Confirmed = true
        });
    }
}