using EventManagement.Model.Data;
using EventManagement.Model.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Repository;

public class EventRepository : IEventRepository
{
    private readonly AppDBContext _db;
    public EventRepository(AppDBContext db) => _db = db;

    public Task<List<CampusEvent>> GetBulletinAsync(DateTime now) =>
        _db.Events.AsNoTracking().Include(e => e.Facility)
            .Where(e => e.Status == EventStatus.Approved && e.EndTime >= now)
            .OrderBy(e => e.StartTime).ToListAsync();

    public Task<List<CampusEvent>> GetCalendarAsync(DateTime from, DateTime to, int? facilityId)
    {
        var q = _db.Events.AsNoTracking().Include(e => e.Facility)
            .Where(e => e.Status != EventStatus.Rejected && e.StartTime < to && e.EndTime > from);
        if (facilityId.HasValue) q = q.Where(e => e.FacilityId == facilityId.Value);
        return q.OrderBy(e => e.StartTime).ToListAsync();
    }

    public Task<List<CampusEvent>> GetByDepartmentAsync(DepartmentType dept, EventStatus status) =>
        _db.Events.AsNoTracking().Include(e => e.Facility)
            .Where(e => e.ApproverDepartment == dept && e.Status == status)
            .OrderBy(e => e.StartTime).ToListAsync();

    public Task<CampusEvent?> GetByIdAsync(int id) =>
        _db.Events.Include(e => e.Facility).Include(e => e.EquipmentItems)
            .FirstOrDefaultAsync(e => e.Id == id);

    public Task<bool> HasConflictAsync(int facilityId, DateTime start, DateTime end, bool approvedOnly, int? excludeId = null)
    {
        var q = _db.Events.Where(e => e.FacilityId == facilityId && e.StartTime < end && start < e.EndTime);
        q = approvedOnly
            ? q.Where(e => e.Status == EventStatus.Approved)
            : q.Where(e => e.Status != EventStatus.Rejected);
        if (excludeId.HasValue) q = q.Where(e => e.Id != excludeId.Value);
        return q.AnyAsync();
    }

    public async Task AddAsync(CampusEvent ev)
    {
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
    }

    public Task SaveAsync() => _db.SaveChangesAsync();
}