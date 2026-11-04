using EventManagement.Model.Data;
using EventManagement.Model.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Repository;

public class RegistrationRepository : IRegistrationRepository
{
    private readonly AppDBContext _db;
    public RegistrationRepository(AppDBContext db) => _db = db;

    public Task<bool> ExistsAsync(int eventId, string email) =>
        _db.Registrations.AnyAsync(r => r.CampusEventId == eventId && r.Email == email);

    public async Task AddAsync(EventRegistration registration)
    {
        _db.Registrations.Add(registration);
        await _db.SaveChangesAsync();
    }
}