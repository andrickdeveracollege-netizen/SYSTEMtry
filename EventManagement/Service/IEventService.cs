using EventManagement.Model.Domain;
using EventManagement.Model.DTO;

namespace EventManagement.Service;

public interface IEventService
{
    Task<List<EventSummaryDto>> GetBulletinAsync();
    Task<List<EventSummaryDto>> GetCalendarAsync(DateTime from, DateTime to, int? facilityId);
    Task<List<FacilityDto>> GetFacilitiesAsync();
    Task<EventSummaryDto> CreateRequestAsync(EventRequestDto dto, string requesterEmail);

    Task<List<EventDetailDto>> GetMineAsync(string requesterEmail);
    Task<EventDetailDto> GetDetailAsync(int id);
    Task<EventDetailDto> UpdateAsync(int id, EventUpdateDto dto, string requesterEmail);
    Task CancelAsync(int id, string requesterEmail);

    // Admin side (works on ONE department's request)
    Task<List<EventReportRowDto>> GetReportAsync(DepartmentType dept);
    Task<List<AdminRequestDto>> GetRequestsForAdminAsync(DepartmentType dept, EventStatus status);
    Task ApproveAsync(int departmentRequestId, DepartmentType dept, string adminEmail);
    Task RejectAsync(int departmentRequestId, DepartmentType dept, string adminEmail, string reason);
    Task<(string PhysicalPath, string ContentType, string DownloadName)> GetEndorsementAsync(int departmentRequestId, DepartmentType dept);
}
