using EventManagement.Mapper;
using EventManagement.Model.Domain;
using EventManagement.Model.DTO;
using EventManagement.Repository;

namespace EventManagement.Service;

public class EventService : IEventService
{
    private static readonly Dictionary<string, string> AllowedFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".pdf"] = "application/pdf",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };
    private const long MaxFileBytes = 5 * 1024 * 1024;

    private readonly IEventRepository _events;
    private readonly IDepartmentRequestRepository _deptRequests;
    private readonly IFacilityRepository _facilities;
    private readonly IEquipmentRepository _equipment;
    private readonly IWebHostEnvironment _env;

    public EventService(
        IEventRepository events,
        IDepartmentRequestRepository deptRequests,
        IFacilityRepository facilities,
        IEquipmentRepository equipment,
        IWebHostEnvironment env)
    {
        _events = events;
        _deptRequests = deptRequests;
        _facilities = facilities;
        _equipment = equipment;
        _env = env;
    }

    private string UploadDir => Path.Combine(_env.ContentRootPath, "App_Data", "endorsements");

    public async Task<List<EventSummaryDto>> GetBulletinAsync() =>
        (await _events.GetBulletinAsync(DateTime.Now)).Select(e => e.ToSummary()).ToList();

    public async Task<List<EventSummaryDto>> GetCalendarAsync(DateTime from, DateTime to, int? facilityId) =>
        (await _events.GetCalendarAsync(from, to, facilityId)).Select(e => e.ToSummary()).ToList();

    public async Task<List<FacilityDto>> GetFacilitiesAsync() =>
        (await _facilities.GetAllAsync()).Select(f => f.ToDto()).ToList();

    // ---------------------------------------------------------------- create

    public async Task<EventSummaryDto> CreateRequestAsync(EventRequestDto dto, string requesterEmail)
    {
        if (!Enum.IsDefined(dto.Role)) throw new ServiceException("Please select your role (Faculty or Student).");
        if (dto.TargetAudience == YearLevel.None || ((int)dto.TargetAudience & ~(int)YearLevel.All) != 0)
            throw new ServiceException("Please select at least one target audience.");
        if (dto.Capacity < 1 || dto.Capacity > 10000) throw new ServiceException("Capacity must be between 1 and 10,000.");
        if (dto.EndTime <= dto.StartTime) throw new ServiceException("End time must be after the start time.");
        if (dto.StartTime < DateTime.Now) throw new ServiceException("The event must start in the future.");
        if (dto.Departments.Count == 0)
            throw new ServiceException("Please tick at least one department you need.");
        if (dto.Departments.Select(d => d.Department).Distinct().Count() != dto.Departments.Count)
            throw new ServiceException("A department was selected more than once.");

        var allFacilities = (await _facilities.GetAllAsync()).ToDictionary(f => f.Id);
        var catalog = (await _equipment.GetAllAsync()).ToDictionary(x => x.Id);

        // Validate everything BEFORE saving any file
        var requests = new List<(DepartmentRequestInputDto Input, DepartmentRequest Entity)>();
        var allFacilityIds = new List<int>();

        foreach (var input in dto.Departments)
        {
            if (!Enum.IsDefined(input.Department)) throw new ServiceException("Invalid department.");
            var office = FacilityMapper.OfficeName(input.Department);

            var facilityIds = input.FacilityIds.Distinct().ToList();
            foreach (var fid in facilityIds)
            {
                if (!allFacilities.TryGetValue(fid, out var f))
                    throw new ServiceException("Selected facility does not exist.");
                if (f.Department != input.Department)
                    throw new ServiceException($"{f.Name} does not belong to {office}.");
            }

            var dr = new DepartmentRequest { Department = input.Department };
            foreach (var fid in facilityIds)
                dr.Facilities.Add(new EventFacility { FacilityId = fid });

            foreach (var req in input.Equipment)
            {
                if (!catalog.TryGetValue(req.EquipmentId, out var eq))
                    throw new ServiceException("Selected equipment does not exist.");
                if (eq.Department != input.Department)
                    throw new ServiceException($"{eq.Name} does not belong to {office}.");
                if (dr.EquipmentItems.Any(i => i.EquipmentId == eq.Id))
                    throw new ServiceException($"{eq.Name} was selected more than once.");
                if (req.Quantity < 1 || req.Quantity > 100)
                    throw new ServiceException($"Quantity for {eq.Name} must be between 1 and 100.");
                if (eq.IsOther && string.IsNullOrWhiteSpace(req.Notes))
                    throw new ServiceException($"Please describe the 'Others' equipment for {office}.");

                dr.EquipmentItems.Add(new EventEquipment
                {
                    EquipmentId = eq.Id,
                    Quantity = req.Quantity,
                    Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim()
                });
            }

            if (dr.Facilities.Count == 0 && dr.EquipmentItems.Count == 0)
                throw new ServiceException($"Pick at least one facility or equipment for {office}, or untick it.");

            // Each department has its own letter. Optional only for College.
            bool hasFile = input.Endorsement is { Length: > 0 };
            if (!hasFile && input.Department != DepartmentType.College)
                throw new ServiceException($"An endorsement letter (image, PDF or DOCX) is required for {office}.");
            if (hasFile) ValidateFile(input.Endorsement!, office);

            allFacilityIds.AddRange(facilityIds);
            requests.Add((input, dr));
        }

        // Double-booking guard (pending OR approved holds the slot)
        var taken = await _events.GetConflictingFacilitiesAsync(allFacilityIds, dto.StartTime, dto.EndTime, approvedOnly: false);
        if (taken.Count > 0)
            throw new ServiceException(
                $"Already booked or pending for this time: {string.Join(", ", taken)}.", 409);

        // Save the letters, then the event
        foreach (var (input, dr) in requests)
        {
            if (input.Endorsement is { Length: > 0 })
                (dr.EndorsementStoredName, dr.EndorsementOriginalName) = await SaveFileAsync(input.Endorsement);
        }

        var ev = new CampusEvent
        {
            Title = dto.Title.Trim(),
            RequesterName = dto.RequesterName.Trim(),
            RequesterEmail = requesterEmail,
            Organization = dto.Organization.Trim(),
            Role = dto.Role,
            TargetAudience = dto.TargetAudience,
            Purpose = dto.Purpose.Trim(),
            Capacity = dto.Capacity,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Status = EventStatus.Pending,
            DepartmentRequests = requests.Select(r => r.Entity).ToList()
        };
        await _events.AddAsync(ev);

        var names = allFacilityIds.Distinct().ToList();
        return new EventSummaryDto(
            ev.Id, ev.Title, ev.Organization, ev.Purpose,
            names, names.Select(id => allFacilities[id].Name).ToList(),
            ev.StartTime, ev.EndTime, ev.TargetAudience, ev.Capacity, 0, ev.Status);
    }

    // ------------------------------------------------------- requester side

    public async Task<List<EventDetailDto>> GetMineAsync(string email) =>
        (await _events.GetMineAsync(email)).Select(e => e.ToDetail()).ToList();

    public async Task<EventDetailDto> GetDetailAsync(int id) =>
        (await _events.GetDetailAsync(id) ?? throw new ServiceException("Event not found.", 404)).ToDetail();

    public async Task<EventDetailDto> UpdateAsync(int id, EventUpdateDto dto, string email)
    {
        var ev = await LoadOwnedAsync(id, email);
        if (ev.Status != EventStatus.Pending)
            throw new ServiceException("Only pending requests can be edited.");
        ev.Title = dto.Title.Trim();
        ev.Purpose = dto.Purpose.Trim();
        ev.Capacity = dto.Capacity;
        await _events.SaveAsync();
        return await GetDetailAsync(id);
    }

    // Soft delete: the event stays in the database with status Cancelled
    public async Task CancelAsync(int id, string email)
    {
        var ev = await LoadOwnedAsync(id, email);
        if (ev.Status is EventStatus.Cancelled or EventStatus.Rejected)
            throw new ServiceException("This event is already closed.");
        if (ev.StartTime < DateTime.Now) throw new ServiceException("The event already started.");
        ev.Status = EventStatus.Cancelled;
        await _events.SaveAsync();
    }

    // ----------------------------------------------------------------- admin

    // Report: per event, how many tickets (registrations) are taken vs capacity
    public async Task<List<EventReportRowDto>> GetReportAsync(DepartmentType dept) =>
        (await _events.GetForDepartmentAsync(dept))
            .Select(e =>
            {
                var registered = e.Registrations.Count(r => !r.Cancelled);
                return new EventReportRowDto(
                    e.Id, e.Title, e.StartTime, e.Status,
                    e.DepartmentRequests.First(d => d.Department == dept).Status,
                    e.Capacity, registered, Math.Max(0, e.Capacity - registered));
            }).ToList();

    public async Task<List<AdminRequestDto>> GetRequestsForAdminAsync(DepartmentType dept, EventStatus status) =>
        (await _deptRequests.GetByDepartmentAsync(dept, status)).Select(d => d.ToAdminDto()).ToList();

    public async Task ApproveAsync(int id, DepartmentType dept, string adminEmail)
    {
        var dr = await LoadForReviewAsync(id, dept);

        var facilityIds = dr.Facilities.Select(f => f.FacilityId).ToList();
        if (facilityIds.Count > 0)
        {
            var taken = await _events.GetConflictingFacilitiesAsync(
                facilityIds, dr.CampusEvent!.StartTime, dr.CampusEvent.EndTime,
                approvedOnly: true, excludeEventId: dr.CampusEventId);
            if (taken.Count > 0)
                throw new ServiceException($"Another approved event already uses: {string.Join(", ", taken)}.", 409);
        }

        dr.Status = EventStatus.Approved;
        dr.ReviewedBy = adminEmail;
        dr.ReviewedAt = DateTime.Now;
        dr.RejectionReason = null;
        RecomputeStatus(dr.CampusEvent!);
        await _deptRequests.SaveAsync();
    }

    public async Task RejectAsync(int id, DepartmentType dept, string adminEmail, string reason)
    {
        var dr = await LoadForReviewAsync(id, dept);
        dr.Status = EventStatus.Rejected;
        dr.ReviewedBy = adminEmail;
        dr.ReviewedAt = DateTime.Now;
        dr.RejectionReason = reason.Trim();

        // One "no" rejects the whole event, so the other offices' pending parts close too
        foreach (var other in dr.CampusEvent!.DepartmentRequests.Where(o => o.Id != dr.Id && o.Status == EventStatus.Pending))
        {
            other.Status = EventStatus.Rejected;
            other.ReviewedBy = adminEmail;
            other.ReviewedAt = DateTime.Now;
            other.RejectionReason = $"Closed because {FacilityMapper.OfficeName(dept)} rejected the event.";
        }
        RecomputeStatus(dr.CampusEvent);
        await _deptRequests.SaveAsync();
    }

    public async Task<(string PhysicalPath, string ContentType, string DownloadName)> GetEndorsementAsync(int id, DepartmentType dept)
    {
        var dr = await _deptRequests.GetByIdAsync(id) ?? throw new ServiceException("Request not found.", 404);
        if (dr.Department != dept) throw new ServiceException("Not allowed.", 403);
        if (dr.EndorsementStoredName == null) throw new ServiceException("No endorsement letter was uploaded.", 404);

        var path = Path.Combine(UploadDir, dr.EndorsementStoredName);
        if (!File.Exists(path)) throw new ServiceException("File is missing on the server.", 404);

        var ext = Path.GetExtension(dr.EndorsementStoredName);
        return (path, AllowedFiles.GetValueOrDefault(ext, "application/octet-stream"),
                dr.EndorsementOriginalName ?? dr.EndorsementStoredName);
    }

    // --------------------------------------------------------------- helpers

    // Any Rejected -> Rejected; all Approved -> Approved; otherwise Pending
    private static void RecomputeStatus(CampusEvent ev)
    {
        var all = ev.DepartmentRequests;
        ev.Status = all.Any(d => d.Status == EventStatus.Rejected) ? EventStatus.Rejected
                  : all.All(d => d.Status == EventStatus.Approved) ? EventStatus.Approved
                  : EventStatus.Pending;
    }

    private async Task<CampusEvent> LoadOwnedAsync(int id, string email)
    {
        var ev = await _events.GetByIdAsync(id) ?? throw new ServiceException("Event not found.", 404);
        if (!string.Equals(ev.RequesterEmail, email, StringComparison.OrdinalIgnoreCase))
            throw new ServiceException("This is not your event request.", 403);
        return ev;
    }

    private async Task<DepartmentRequest> LoadForReviewAsync(int id, DepartmentType dept)
    {
        var dr = await _deptRequests.GetByIdAsync(id) ?? throw new ServiceException("Request not found.", 404);
        if (dr.Department != dept) throw new ServiceException("This request belongs to another office.", 403);
        if (dr.CampusEvent!.Status == EventStatus.Cancelled) throw new ServiceException("The requester cancelled this event.");
        if (dr.Status != EventStatus.Pending) throw new ServiceException("This request was already reviewed.");
        return dr;
    }

    private static void ValidateFile(IFormFile file, string office)
    {
        var ext = Path.GetExtension(file.FileName);
        if (!AllowedFiles.ContainsKey(ext))
            throw new ServiceException($"The letter for {office} must be an image (JPG/PNG), PDF or DOCX file.");
        if (file.Length > MaxFileBytes)
            throw new ServiceException($"The letter for {office} must be 5 MB or smaller.");
    }

    private async Task<(string stored, string original)> SaveFileAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName);
        Directory.CreateDirectory(UploadDir);
        var stored = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        await using var fs = File.Create(Path.Combine(UploadDir, stored));
        await file.CopyToAsync(fs);
        return (stored, Path.GetFileName(file.FileName));
    }
}
