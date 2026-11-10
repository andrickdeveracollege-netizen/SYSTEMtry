namespace EventManagement.Repository;

// A facility that is already booked in the requested time window.
// PendingGuest = the booking is a guest request that is still pending (school events may take its place).
public record FacilityConflict(string FacilityName, int EventId, bool PendingGuest);
