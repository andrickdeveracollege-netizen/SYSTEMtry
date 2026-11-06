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

        // Year level must match the event's target audience
        if ((ev.TargetAudience & dto.YearLevel) == 0)
            throw new ServiceException($"This event is only open to: {ev.TargetAudience}. Your year level ({dto.YearLevel}) is not included.");

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _registrations.ExistsAsync(ev.Id, email))
            throw new ServiceException("You are already registered for this event.", 409);

        // Capacity rule: registrations can never exceed the event capacity
        if (await _registrations.CountActiveAsync(ev.Id) >= ev.Capacity)
            throw new ServiceException("This event is already full.", 409);

        var previous = await _registrations.GetAsync(ev.Id, email);
        if (previous != null)   // re-register after a cancellation
        {
            previous.Cancelled = false;
            previous.FullName = dto.FullName.Trim();
            previous.YearLevel = dto.YearLevel;
            previous.RegisteredAt = DateTime.Now;
            await _registrations.SaveAsync();
            return;
        }

        await _registrations.AddAsync(new EventRegistration
        {
            CampusEventId = ev.Id,
            Email = email,
            FullName = dto.FullName.Trim(),
            YearLevel = dto.YearLevel,
            Confirmed = true
        });
    }

    public async Task<List<MyRegistrationDto>> GetMineAsync(string email) =>
        (await _registrations.GetByEmailAsync(email.ToLowerInvariant()))
            .Select(r => new MyRegistrationDto(
                r.Id, r.CampusEventId, r.CampusEvent!.Title, r.CampusEvent.StartTime, r.YearLevel, r.Cancelled))
            .ToList();

    public async Task CancelAsync(int registrationId, string email)
    {
        var reg = await _registrations.GetByIdAsync(registrationId) ?? throw new ServiceException("Registration not found.", 404);
        if (!string.Equals(reg.Email, email, StringComparison.OrdinalIgnoreCase))
            throw new ServiceException("This is not your registration.", 403);
        if (reg.Cancelled) throw new ServiceException("This registration is already cancelled.");
        if (reg.CampusEvent!.StartTime < DateTime.Now) throw new ServiceException("The event already started.");

        reg.Cancelled = true;
        await _registrations.SaveAsync();
    }
}
