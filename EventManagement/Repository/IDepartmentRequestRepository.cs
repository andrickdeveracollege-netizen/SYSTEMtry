using EventManagement.Model.Domain;

namespace EventManagement.Repository;

public interface IDepartmentRequestRepository
{
    Task<List<DepartmentRequest>> GetByDepartmentAsync(DepartmentType dept, EventStatus status);
    // Tracked, with CampusEvent, Facilities and the event's other DepartmentRequests loaded
    Task<DepartmentRequest?> GetByIdAsync(int id);
    Task SaveAsync();
}
