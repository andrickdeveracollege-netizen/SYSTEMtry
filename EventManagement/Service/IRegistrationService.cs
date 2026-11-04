using EventManagement.Model.DTO;

namespace EventManagement.Service;

public interface IRegistrationService
{
    Task RegisterAsync(RegistrationDto dto);
}