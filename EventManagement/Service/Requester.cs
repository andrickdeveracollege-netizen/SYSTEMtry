using EventManagement.Model.Domain;

namespace EventManagement.Service;

// Who is making the request: a signed-in school account, or a guest (outsider).
public record Requester(string Email, RequesterRole Role, bool IsGuest, string? Phone = null);
