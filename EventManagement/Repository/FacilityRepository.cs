using EventManagement.Model.Data;
using EventManagement.Model.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Repository;

public class FacilityRepository : IFacilityRepository
{
    private readonly AppDBContext _db;
    public FacilityRepository(AppDBContext db) => _db = db;

    public Task<List<Facility>> GetAllAsync() =>
        _db.Facilities.AsNoTracking().OrderBy(f => f.Department).ThenBy(f => f.Id).ToListAsync();

    public Task<Facility?> GetByIdAsync(int id) => _db.Facilities.FirstOrDefaultAsync(f => f.Id == id);
}