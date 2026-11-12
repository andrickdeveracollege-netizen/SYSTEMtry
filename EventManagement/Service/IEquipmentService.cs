using EventManagement.Model.DTO;

namespace EventManagement.Service;

public interface IEquipmentService
{
    Task<List<EquipmentDto>> GetCatalogAsync();
}
