using EventManagement.Model.Data;
using EventManagement.Model.Domain;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Repository;

public class UserRepository : IUserRepository
{
    private readonly AppDBContext _db;
    public UserRepository(AppDBContext db) => _db = db;

    public Task<User?> GetByEmailAsync(string email) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task AddAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }
}