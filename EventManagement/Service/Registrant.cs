using EventManagement.Model.Domain;

namespace EventManagement.Service;

// Who is registering for an event. Level comes from the email (students) or is Outsider (guests).
public record Registrant(string Email, string FullName, RequesterRole Role, YearLevel Level);
