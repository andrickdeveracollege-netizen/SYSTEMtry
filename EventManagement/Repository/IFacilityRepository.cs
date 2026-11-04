using EventManagement.Model.Domain;

namespace EventManagement.Repository;

public interface IFacilityRepository
{
    Task<List<Facility>> GetAllAsync();
    Task<Facility?> GetByIdAsync(int id);
}