using EventManagement.Model.Data;
using EventManagement.Model.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Repository;

public class DepartmentRequestRepository : IDepartmentRequestRepository
{
    private readonly AppDBContext _db;
    public DepartmentRequestRepository(AppDBContext db) => _db = db;

    public Task<List<DepartmentRequest>> GetByDepartmentAsync(DepartmentType dept, EventStatus status) =>
        _db.DepartmentRequests.AsNoTracking()
            .Include(d => d.CampusEvent)
            .Include(d => d.Facilities).ThenInclude(f => f.Facility)
            .Include(d => d.EquipmentItems).ThenInclude(i => i.Equipment)
            .Where(d => d.Department == dept && d.Status == status && d.CampusEvent!.Status != EventStatus.Cancelled)
            .OrderBy(d => d.CampusEvent!.StartTime)
            .ToListAsync();

    public Task<DepartmentRequest?> GetByIdAsync(int id) =>
        _db.DepartmentRequests
            .Include(d => d.CampusEvent).ThenInclude(e => e!.DepartmentRequests)
            .Include(d => d.Facilities).ThenInclude(f => f.Facility)
            .FirstOrDefaultAsync(d => d.Id == id);

    public Task SaveAsync() => _db.SaveChangesAsync();
}
