using EventManagement.Model.Domain;

namespace EventManagement.Repository;

public interface IEventRepository
{
    Task<List<CampusEvent>> GetBulletinAsync(DateTime now);
    Task<List<CampusEvent>> GetCalendarAsync(DateTime from, DateTime to, int? facilityId);
    Task<List<CampusEvent>> GetMineAsync(string requesterEmail);
    Task<List<CampusEvent>> GetForDepartmentAsync(DepartmentType dept);
    Task<CampusEvent?> GetDetailAsync(int id);            // read-only, everything loaded
    Task<CampusEvent?> GetByIdAsync(int id);              // tracked, with DepartmentRequests
    // Names of facilities already taken in the given window (pending or approved)
    Task<List<string>> GetConflictingFacilitiesAsync(IEnumerable<int> facilityIds, DateTime start, DateTime end, bool approvedOnly, int? excludeEventId = null);
    Task AddAsync(CampusEvent ev);
    Task SaveAsync();
}
