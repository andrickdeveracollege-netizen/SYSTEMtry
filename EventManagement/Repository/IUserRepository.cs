using EventManagement.Model.Domain;

namespace EventManagement.Repository;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
}