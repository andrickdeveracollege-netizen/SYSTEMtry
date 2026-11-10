using EventManagement.Model.Domain;
using EventManagement.Model.DTO;

namespace EventManagement.Service;

public interface IEventService
{
    Task<List<EventSummaryDto>> GetBulletinAsync();
    Task<List<EventSummaryDto>> GetCalendarAsync(DateTime from, DateTime to, int? facilityId);
    Task<List<FacilityDto>> GetFacilitiesAsync();

    // Requests (school accounts and guests share the same rules, guests are weekend-only)
    Task<EventCreatedDto> CreateRequestAsync(EventRequestDto dto, Requester who);
    Task<EventCreatedDto> CreateInstitutionalAsync(InstitutionalEventDto dto, string adminEmail, string adminName);

    // Requester manages own event
    Task<List<EventDetailDto>> GetMineAsync(string requesterEmail);
    Task<EventDetailDto> GetDetailAsync(int id);
    Task<EventDetailDto> UpdateAsync(int id, EventUpdateDto dto, string requesterEmail);
    Task<EventDetailDto> RescheduleAsync(int id, RescheduleDto dto, string requesterEmail);
    Task CancelAsync(int id, string requesterEmail);

    // Guest manages own event with email + manage code
    Task<EventDetailDto> GetGuestEventAsync(int id, string email, string code);
    Task<EventDetailDto> RescheduleGuestAsync(int id, RescheduleDto dto, string email, string code);
    Task CancelGuestAsync(int id, string email, string code);

    // Admin side. dept = the admin's office, or null for the super admin (any office)
    Task<List<EventReportRowDto>> GetReportAsync(DepartmentType dept);
    Task<List<AdminRequestDto>> GetRequestsForAdminAsync(DepartmentType dept, EventStatus status);
    Task ApproveAsync(int departmentRequestId, DepartmentType? dept, string adminEmail);
    Task RejectAsync(int departmentRequestId, DepartmentType? dept, string adminEmail, string reason);
    Task AdminCancelEventAsync(int eventId, DepartmentType? dept);
    Task<(string PhysicalPath, string ContentType, string DownloadName)> GetEndorsementAsync(int departmentRequestId, DepartmentType? dept);
}
