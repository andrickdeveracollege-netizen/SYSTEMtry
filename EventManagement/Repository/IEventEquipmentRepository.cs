using EventManagement.Model.Domain;

namespace EventManagement.Repository;

public interface IEventEquipmentRepository
{
    Task<List<EventEquipment>> GetByDepartmentAsync(DepartmentType dept, EventStatus status);
    Task<EventEquipment?> GetByIdAsync(int id);
    Task SaveAsync();
}