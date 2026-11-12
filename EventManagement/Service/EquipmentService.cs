using EventManagement.Mapper;
using EventManagement.Model.DTO;
using EventManagement.Repository;

namespace EventManagement.Service;

public class EquipmentService : IEquipmentService
{
    private readonly IEquipmentRepository _equipment;
    public EquipmentService(IEquipmentRepository equipment) => _equipment = equipment;

    public async Task<List<EquipmentDto>> GetCatalogAsync() =>
        (await _equipment.GetAllAsync()).Select(e => e.ToDto()).ToList();
}
