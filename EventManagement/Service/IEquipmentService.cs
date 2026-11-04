using EventManagement.Model.Domain;
using EventManagement.Model.DTO;

namespace EventManagement.Service;

public interface IEquipmentService
{
    Task<List<EquipmentDto>> GetCatalogAsync();
    Task<List<AdminEquipmentRequestDto>> GetRequestsForAdminAsync(DepartmentType dept, EventStatus status);
    Task ApproveAsync(int id, DepartmentType dept, string adminEmail);
    Task RejectAsync(int id, DepartmentType dept, string adminEmail, string reason);
}