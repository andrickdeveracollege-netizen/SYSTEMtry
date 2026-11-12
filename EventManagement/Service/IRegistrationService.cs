using EventManagement.Model.DTO;

namespace EventManagement.Service;

public interface IRegistrationService
{
    // Signed-in school account: name, email and year level come from the account
    Task RegisterMemberAsync(int eventId, bool confirmed, Registrant who);
    // Outsider: email is required, year level is always "Outsider"
    Task RegisterGuestAsync(RegistrationDto dto);
    Task<List<MyRegistrationDto>> GetMineAsync(string email);
    Task CancelAsync(int registrationId, string email);
}
