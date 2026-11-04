using EventManagement.Model.Domain;

namespace EventManagement.Repository;

public interface IEventRepository
{
    Task<List<CampusEvent>> GetBulletinAsync(DateTime now);
    Task<List<CampusEvent>> GetCalendarAsync(DateTime from, DateTime to, int? facilityId);
    Task<List<CampusEvent>> GetByDepartmentAsync(DepartmentType dept, EventStatus status);
    Task<CampusEvent?> GetByIdAsync(int id);
    Task<bool> HasConflictAsync(int facilityId, DateTime start, DateTime end, bool approvedOnly, int? excludeId = null);
    Task AddAsync(CampusEvent ev);
    Task SaveAsync();
}