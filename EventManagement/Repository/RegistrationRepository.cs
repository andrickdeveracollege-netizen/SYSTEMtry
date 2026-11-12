using EventManagement.Model.Data;
using EventManagement.Model.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Repository;

public class RegistrationRepository : IRegistrationRepository
{
    private readonly AppDBContext _db;
    public RegistrationRepository(AppDBContext db) => _db = db;

    public Task<bool> ExistsAsync(int eventId, string email) =>
        _db.Registrations.AnyAsync(r => r.CampusEventId == eventId && r.Email == email && !r.Cancelled);

    public Task<EventRegistration?> GetAsync(int eventId, string email) =>
        _db.Registrations.FirstOrDefaultAsync(r => r.CampusEventId == eventId && r.Email == email);

    public Task<EventRegistration?> GetByIdAsync(int id) =>
        _db.Registrations.Include(r => r.CampusEvent).FirstOrDefaultAsync(r => r.Id == id);

    public Task<int> CountActiveAsync(int eventId) =>
        _db.Registrations.CountAsync(r => r.CampusEventId == eventId && !r.Cancelled);

    public Task<List<EventRegistration>> GetByEmailAsync(string email) =>
        _db.Registrations.AsNoTracking().Include(r => r.CampusEvent)
            .Where(r => r.Email == email).OrderByDescending(r => r.RegisteredAt).ToListAsync();

    public async Task AddAsync(EventRegistration registration)
    {
        _db.Registrations.Add(registration);
        await _db.SaveChangesAsync();
    }

    public Task SaveAsync() => _db.SaveChangesAsync();
}
