using EventManagement.Model.Domain;

namespace EventManagement.Repository;

public interface IEquipmentRepository
{
    Task<List<Equipment>> GetAllAsync();
}