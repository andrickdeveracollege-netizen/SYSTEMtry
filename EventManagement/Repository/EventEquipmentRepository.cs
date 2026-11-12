using EventManagement.Model.Data;
using EventManagement.Model.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Repository;

public class EventEquipmentRepository : IEventEquipmentRepository
{
    private readonly AppDBContext _db;
    public EventEquipmentRepository(AppDBContext db) => _db = db;

    public Task<List<EventEquipment>> GetByDepartmentAsync(DepartmentType dept, EventStatus status) =>
        _db.EventEquipments.AsNoTracking()
            .Include(x => x.Equipment)
            .Include(x => x.DepartmentRequest).ThenInclude(r => r!.CampusEvent)
            .Include(x => x.DepartmentRequest).ThenInclude(r => r!.Facilities).ThenInclude(f => f.Facility)
            .Where(x => x.Equipment != null
                     && x.Equipment.Department == dept
                     && x.DepartmentRequest != null
                     && x.DepartmentRequest.Status == status)
            .OrderBy(x => x.DepartmentRequest!.CampusEvent!.StartTime)
            .ToListAsync();

    public Task<EventEquipment?> GetByIdAsync(int id) =>
        _db.EventEquipments
            .Include(x => x.Equipment)
            .Include(x => x.DepartmentRequest).ThenInclude(r => r!.CampusEvent)
            .FirstOrDefaultAsync(x => x.Id == id);

    public Task SaveAsync() => _db.SaveChangesAsync();
}