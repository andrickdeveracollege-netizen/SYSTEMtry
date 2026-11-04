using EventManagement.Model.Domain;

namespace EventManagement.Repository;

public interface IRegistrationRepository
{
    Task<bool> ExistsAsync(int eventId, string email);
    Task AddAsync(EventRegistration registration);
}