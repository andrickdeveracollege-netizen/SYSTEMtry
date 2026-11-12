using EventManagement.Model.Domain;

namespace EventManagement.Model.DTO;

public record DepartmentStatusDto(string Office, EventStatus Status, string? RejectionReason, string? Note);
