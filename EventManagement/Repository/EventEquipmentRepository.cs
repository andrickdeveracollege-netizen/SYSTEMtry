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
            .Include(x => x.CampusEvent).ThenInclude(c => c!.Facility)
            .Where(x => x.Equipment!.Department == dept && x.Status == status)
            .OrderBy(x => x.CampusEvent!.StartTime)
            .ToListAsync();

    public Task<EventEquipment?> GetByIdAsync(int id) =>
        _db.EventEquipments
            .Include(x => x.Equipment)
            .Include(x => x.CampusEvent)
            .FirstOrDefaultAsync(x => x.Id == id);

    public Task SaveAsync() => _db.SaveChangesAsync();
}