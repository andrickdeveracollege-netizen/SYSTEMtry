using EventManagement.Model.Domain;
using EventManagement.Model.DTO;
using EventManagement.Repository;

namespace EventManagement.Service;

// Used only by the super admin (admin@ustangelicum.edu.ph)
public class UserAdminService : IUserAdminService
{
    private readonly IUserRepository _users;
    private readonly IConfiguration _config;

    public UserAdminService(IUserRepository users, IConfiguration config)
    {
        _users = users;
        _config = config;
    }

    public async Task<List<UserDto>> GetUsersAsync() =>
        (await _users.GetAllAsync())
            .Select(u => new UserDto(u.Id, u.Email, u.FullName, u.Role, u.Position, u.Level, u.AdminDepartment))
            .ToList();

    // Back to the default password (Auth:DefaultPassword). The user should change it after signing in.
    public async Task ResetPasswordAsync(Guid id)
    {
        var user = await _users.GetByIdAsync(id) ?? throw new ServiceException("User not found.", 404);
        var defaultPassword = _config["Auth:DefaultPassword"];
        if (string.IsNullOrWhiteSpace(defaultPassword))
            throw new ServiceException("Auth:DefaultPassword is not configured.", 500);

        user.PasswordHash = PasswordHasher.Hash(defaultPassword);
        await _users.SaveAsync();
    }

    // Only faculty / staff can be made Administrator (and back). Students keep their level.
    public async Task SetPositionAsync(Guid id, SetPositionDto dto)
    {
        var user = await _users.GetByIdAsync(id) ?? throw new ServiceException("User not found.", 404);
        if (user.Role != UserRole.User) throw new ServiceException("Office admins and the super admin have no position.");
        if (user.Position == RequesterRole.Student) throw new ServiceException("Students cannot be made Administrator.");
        if (dto.Position is not (RequesterRole.Faculty or RequesterRole.Administrator))
            throw new ServiceException("Position must be Faculty or Administrator.");

        user.Position = dto.Position;
        await _users.SaveAsync();
    }
}
