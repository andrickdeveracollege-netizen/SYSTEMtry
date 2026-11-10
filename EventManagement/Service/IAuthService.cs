using EventManagement.Model.DTO;

namespace EventManagement.Service;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterAccountDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task ChangePasswordAsync(string email, ChangePasswordDto dto);
}
