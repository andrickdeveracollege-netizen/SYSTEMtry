using EventManagement.Model.Domain;

namespace EventManagement.Repository;

public interface IRegistrationRepository
{
    Task<bool> ExistsAsync(int eventId, string email);                 // active (not cancelled) only
    Task<EventRegistration?> GetAsync(int eventId, string email);       // tracked, includes cancelled
    Task<EventRegistration?> GetByIdAsync(int id);                      // tracked
    Task<int> CountActiveAsync(int eventId);
    Task<List<EventRegistration>> GetByEmailAsync(string email);        // with CampusEvent
    Task AddAsync(EventRegistration registration);
    Task SaveAsync();
}
