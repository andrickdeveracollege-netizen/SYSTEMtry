using EventManagement.Model.Domain;
using EventManagement.Model.DTO;

namespace EventManagement.Service;

public interface IEventService
{
    Task<List<EventSummaryDto>> GetBulletinAsync();
    Task<List<EventSummaryDto>> GetCalendarAsync(DateTime from, DateTime to, int? facilityId);
    Task<List<FacilityDto>> GetFacilitiesAsync();
    Task<EventSummaryDto> CreateRequestAsync(EventRequestDto dto, string requesterEmail);
    Task<List<AdminRequestDto>> GetRequestsForAdminAsync(DepartmentType dept, EventStatus status);
    Task ApproveAsync(int id, DepartmentType dept, string adminEmail);
    Task RejectAsync(int id, DepartmentType dept, string adminEmail, string reason);
    Task<(string PhysicalPath, string ContentType, string DownloadName)> GetEndorsementAsync(int id, DepartmentType dept);
}