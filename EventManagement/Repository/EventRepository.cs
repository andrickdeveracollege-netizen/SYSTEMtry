using EventManagement.Model.Data;
using EventManagement.Model.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Repository;

public class EventRepository : IEventRepository
{
    private readonly AppDBContext _db;
    public EventRepository(AppDBContext db) => _db = db;

    private IQueryable<CampusEvent> Full() =>
        _db.Events.AsNoTracking()
            .Include(e => e.Registrations)
            .Include(e => e.DepartmentRequests).ThenInclude(d => d.Facilities).ThenInclude(f => f.Facility);

    public Task<List<CampusEvent>> GetBulletinAsync(DateTime now) =>
        Full().Where(e => e.Status == EventStatus.Approved && e.EndTime >= now)
              .OrderBy(e => e.StartTime).ToListAsync();

    public Task<List<CampusEvent>> GetCalendarAsync(DateTime from, DateTime to, int? facilityId)
    {
        var q = Full().Where(e => e.Status != EventStatus.Rejected && e.Status != EventStatus.Cancelled
                                  && e.StartTime < to && e.EndTime > from);
        if (facilityId.HasValue)
            q = q.Where(e => e.DepartmentRequests.Any(d => d.Facilities.Any(f => f.FacilityId == facilityId.Value)));
        return q.OrderBy(e => e.StartTime).ToListAsync();
    }

    public Task<List<CampusEvent>> GetMineAsync(string requesterEmail) =>
        Full().Where(e => e.RequesterEmail == requesterEmail)
              .OrderByDescending(e => e.CreatedAt).ToListAsync();

    public Task<List<CampusEvent>> GetForDepartmentAsync(DepartmentType dept) =>
        Full().Where(e => e.DepartmentRequests.Any(d => d.Department == dept))
              .OrderBy(e => e.StartTime).ToListAsync();

    public Task<CampusEvent?> GetDetailAsync(int id) =>
        Full().FirstOrDefaultAsync(e => e.Id == id);

    public Task<CampusEvent?> GetByIdAsync(int id) =>
        _db.Events
            .Include(e => e.DepartmentRequests).ThenInclude(d => d.Facilities).ThenInclude(f => f.Facility)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<List<FacilityConflict>> GetConflictsAsync(
        IEnumerable<int> facilityIds, DateTime start, DateTime end, bool approvedOnly, int? excludeEventId = null)
    {
        var ids = facilityIds.ToList();
        if (ids.Count == 0) return new List<FacilityConflict>();

        var q = _db.EventFacilities.Where(ef =>
            ids.Contains(ef.FacilityId)
            && ef.DepartmentRequest!.CampusEvent!.StartTime < end
            && start < ef.DepartmentRequest.CampusEvent.EndTime
            && ef.DepartmentRequest.CampusEvent.Status != EventStatus.Rejected
            && ef.DepartmentRequest.CampusEvent.Status != EventStatus.Cancelled
            && ef.DepartmentRequest.Status != EventStatus.Rejected);

        if (approvedOnly) q = q.Where(ef => ef.DepartmentRequest!.Status == EventStatus.Approved);
        if (excludeEventId.HasValue) q = q.Where(ef => ef.DepartmentRequest!.CampusEventId != excludeEventId.Value);

        var rows = await q.Select(ef => new
        {
            Name = ef.Facility!.Name,
            EventId = ef.DepartmentRequest!.CampusEventId,
            PendingGuest = ef.DepartmentRequest.CampusEvent!.IsGuest
                           && ef.DepartmentRequest.CampusEvent.Status == EventStatus.Pending
        }).ToListAsync();

        return rows.Select(r => new FacilityConflict(r.Name, r.EventId, r.PendingGuest)).ToList();
    }

    public async Task AddAsync(CampusEvent ev)
    {
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
    }

    public Task SaveAsync() => _db.SaveChangesAsync();
}
