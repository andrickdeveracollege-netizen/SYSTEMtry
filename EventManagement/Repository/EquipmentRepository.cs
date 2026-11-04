using EventManagement.Model.Data;
using EventManagement.Model.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Repository;

public class EquipmentRepository : IEquipmentRepository
{
    private readonly AppDBContext _db;
    public EquipmentRepository(AppDBContext db) => _db = db;

    public Task<List<Equipment>> GetAllAsync() =>
        _db.Equipments.AsNoTracking().OrderBy(e => e.Department).ThenBy(e => e.Id).ToListAsync();
}