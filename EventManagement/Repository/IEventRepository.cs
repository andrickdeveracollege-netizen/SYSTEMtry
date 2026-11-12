using EventManagement.Model.Domain;

namespace EventManagement.Repository;

public interface IEventRepository
{
    Task<List<CampusEvent>> GetBulletinAsync(DateTime now);
    Task<List<CampusEvent>> GetCalendarAsync(DateTime from, DateTime to, int? facilityId);
    Task<List<CampusEvent>> GetMineAsync(string requesterEmail);
    Task<List<CampusEvent>> GetForDepartmentAsync(DepartmentType dept);
    Task<CampusEvent?> GetDetailAsync(int id);            // read-only, everything loaded
    Task<CampusEvent?> GetByIdAsync(int id);              // tracked, with DepartmentRequests + Facilities
    // Which facilities are already taken in the given window, and by which event
    Task<List<FacilityConflict>> GetConflictsAsync(IEnumerable<int> facilityIds, DateTime start, DateTime end, bool approvedOnly, int? excludeEventId = null);
    Task AddAsync(CampusEvent ev);
    Task SaveAsync();
}
