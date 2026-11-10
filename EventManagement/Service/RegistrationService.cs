using EventManagement.Model.Domain;
using EventManagement.Model.DTO;
using EventManagement.Repository;

namespace EventManagement.Service;

public class RegistrationService : IRegistrationService
{
    private readonly IEventRepository _events;
    private readonly IRegistrationRepository _registrations;
    private readonly IConfiguration _config;

    public RegistrationService(IEventRepository events, IRegistrationRepository registrations, IConfiguration config)
    {
        _events = events;
        _registrations = registrations;
        _config = config;
    }

    public Task RegisterMemberAsync(int eventId, bool confirmed, Registrant who) =>
        RegisterCoreAsync(eventId, confirmed, who);

    public Task RegisterGuestAsync(RegistrationDto dto)
    {
        var domain = _config["Auth:AllowedEmailDomain"];
        if (!string.IsNullOrWhiteSpace(domain) &&
            dto.Email.Trim().EndsWith("@" + domain.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ServiceException("School accounts must sign in to register.");

        return RegisterCoreAsync(dto.EventId, dto.Confirmed,
            new Registrant(dto.Email, dto.FullName, RequesterRole.Guest, YearLevel.Outsider));
    }

    private async Task RegisterCoreAsync(int eventId, bool confirmed, Registrant who)
    {
        if (!confirmed) throw new ServiceException("Please confirm your registration.");

        var ev = await _events.GetByIdAsync(eventId) ?? throw new ServiceException("Event not found.", 404);
        if (ev.Status != EventStatus.Approved) throw new ServiceException("This event is not open for registration.");
        if (!ev.RequiresRegistration) throw new ServiceException("This is an institutional event. No registration is needed.");
        if (ev.EndTime < DateTime.Now) throw new ServiceException("This event has already ended.");

        // Target audience rule
        switch (who.Role)
        {
            case RequesterRole.Student when (ev.TargetAudience & who.Level) == 0:
                throw new ServiceException($"This event is only open to: {ev.TargetAudience}. Your level ({who.Level}) is not included.");
            case RequesterRole.Guest when !ev.TargetAudience.HasFlag(YearLevel.Outsider):
                throw new ServiceException($"This event is not open to outsiders. It is open to: {ev.TargetAudience}.");
        }

        var email = who.Email.Trim().ToLowerInvariant();
        if (await _registrations.ExistsAsync(ev.Id, email))
            throw new ServiceException("You are already registered for this event.", 409);

        // Capacity rule: registrations can never exceed the event capacity
        if (await _registrations.CountActiveAsync(ev.Id) >= ev.Capacity)
            throw new ServiceException("This event is already full.", 409);

        var previous = await _registrations.GetAsync(ev.Id, email);
        if (previous != null)   // re-register after a cancellation
        {
            previous.Cancelled = false;
            previous.FullName = who.FullName.Trim();
            previous.YearLevel = who.Level;
            previous.RegisteredAt = DateTime.Now;
            await _registrations.SaveAsync();
            return;
        }

        await _registrations.AddAsync(new EventRegistration
        {
            CampusEventId = ev.Id,
            Email = email,
            FullName = who.FullName.Trim(),
            YearLevel = who.Level,
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
