using EventManagement.Model.DTO;

namespace EventManagement.Service;

public interface IRegistrationService
{
    Task RegisterAsync(RegistrationDto dto);
    Task<List<MyRegistrationDto>> GetMineAsync(string email);
    Task CancelAsync(int registrationId, string email);
}
