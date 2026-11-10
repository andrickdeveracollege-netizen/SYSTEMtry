using EventManagement.Model.DTO;

namespace EventManagement.Service;

public interface IUserAdminService
{
    Task<List<UserDto>> GetUsersAsync();
    Task ResetPasswordAsync(Guid id);
    Task SetPositionAsync(Guid id, SetPositionDto dto);
}
