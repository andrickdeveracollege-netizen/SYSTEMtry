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
    private readonly IConfiguration _config;

    public EventService(
        IEventRepository events,
        IDepartmentRequestRepository deptRequests,
        IFacilityRepository facilities,
        IEquipmentRepository equipment,
        IWebHostEnvironment env,
        IConfiguration config)
    {
        _events = events;
        _deptRequests = deptRequests;
        _facilities = facilities;
        _equipment = equipment;
        _env = env;
        _config = config;
    }

    private string UploadDir => Path.Combine(_env.ContentRootPath, "App_Data", "endorsements");

    public async Task<List<EventSummaryDto>> GetBulletinAsync() =>
        (await _events.GetBulletinAsync(DateTime.Now)).Select(e => e.ToSummary()).ToList();

    public async Task<List<EventSummaryDto>> GetCalendarAsync(DateTime from, DateTime to, int? facilityId) =>
        (await _events.GetCalendarAsync(from, to, facilityId)).Select(e => e.ToSummary()).ToList();

    public async Task<List<FacilityDto>> GetFacilitiesAsync() =>
        (await _facilities.GetAllAsync()).Select(f => f.ToDto()).ToList();

    // ---------------------------------------------------------------- create

    public async Task<EventCreatedDto> CreateRequestAsync(EventRequestDto dto, Requester who)
    {
        if (who.IsGuest) RejectSchoolEmail(who.Email);

        if (dto.TargetAudience == YearLevel.None || ((int)dto.TargetAudience & ~(int)YearLevel.All) != 0)
            throw new ServiceException("Please select at least one target audience.");
        if (dto.Capacity < 1 || dto.Capacity > 10000)
            throw new ServiceException("Number of attendees must be between 1 and 10,000.");
        ValidateTimes(dto.StartTime, dto.EndTime, who.IsGuest);
        if (dto.Departments.Count == 0)
            throw new ServiceException("Please tick at least one department you need.");
        if (dto.Departments.Select(d => d.Department).Distinct().Count() != dto.Departments.Count)
            throw new ServiceException("A department was selected more than once.");

        var allFacilities = (await _facilities.GetAllAsync()).ToDictionary(f => f.Id);
        var catalog = (await _equipment.GetAllAsync()).ToDictionary(x => x.Id);

        // Validate everything BEFORE saving any file
        var requests = new List<(DepartmentRequestInputDto Input, DepartmentRequest Entity)>();
        var bookable = new List<int>();          // real facilities (not "Others") that can clash with other events
        var shownNames = new List<string>();

        foreach (var input in dto.Departments)
        {
            if (!Enum.IsDefined(input.Department)) throw new ServiceException("Invalid department.");
            var office = FacilityMapper.OfficeName(input.Department);
            var otherName = input.OtherFacilityName?.Trim();

            var dr = new DepartmentRequest { Department = input.Department };
            foreach (var fid in input.FacilityIds.Distinct())
            {
                if (!allFacilities.TryGetValue(fid, out var f))
                    throw new ServiceException("Selected facility does not exist.");
                if (f.Department != input.Department)
                    throw new ServiceException($"{f.Name} does not belong to {office}.");

                if (f.IsOther)
                {
                    if (string.IsNullOrWhiteSpace(otherName))
                        throw new ServiceException($"Please type the facility name for 'Others' under {office}.");
                    dr.Facilities.Add(new EventFacility { FacilityId = f.Id, OtherName = otherName });
                    shownNames.Add($"Others: {otherName}");
                    continue;
                }

                // Small rooms are not allowed for big events
                if (f.Capacity.HasValue && f.Capacity.Value < dto.Capacity)
                    throw new ServiceException($"{f.Name} only fits {f.Capacity} people, but you expect {dto.Capacity}. Choose a bigger facility.");

                dr.Facilities.Add(new EventFacility { FacilityId = f.Id });
                bookable.Add(f.Id);
                shownNames.Add(f.Name);
            }

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

            requests.Add((input, dr));
        }

        // Double-booking guard. School events may replace PENDING guest requests, nothing else.
        await ResolveConflictsAsync(bookable, dto.StartTime, dto.EndTime, who.IsGuest, null);

        // Save the letters, then the event
        foreach (var (input, dr) in requests)
        {
            if (input.Endorsement is { Length: > 0 })
                (dr.EndorsementStoredName, dr.EndorsementOriginalName) = await SaveFileAsync(input.Endorsement);
        }

        var institutional = who.Role == RequesterRole.Administrator;
        var ev = new CampusEvent
        {
            Title = dto.Title.Trim(),
            RequesterName = dto.RequesterName.Trim(),
            RequesterEmail = who.Email.Trim().ToLowerInvariant(),
            RequesterPhone = who.Phone,
            Organization = dto.Organization.Trim(),
            Role = who.Role,
            TargetAudience = dto.TargetAudience,
            Purpose = dto.Purpose.Trim(),
            Capacity = dto.Capacity,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            IsGuest = who.IsGuest,
            IsInstitutional = institutional,
            RequiresRegistration = !institutional,
            ManageCode = NewManageCode(),
            Status = EventStatus.Pending,
            DepartmentRequests = requests.Select(r => r.Entity).ToList()
        };
        await _events.AddAsync(ev);

        return new EventCreatedDto(Summary(ev, bookable, shownNames), ev.ManageCode);
    }

    // Super admin posts a school-wide event: approved right away, nobody needs to register
    public async Task<EventCreatedDto> CreateInstitutionalAsync(InstitutionalEventDto dto, string adminEmail, string adminName)
    {
        if (dto.TargetAudience == YearLevel.None || ((int)dto.TargetAudience & ~(int)YearLevel.All) != 0)
            throw new ServiceException("Please select at least one target audience.");
        ValidateTimes(dto.StartTime, dto.EndTime, isGuest: false);

        var allFacilities = (await _facilities.GetAllAsync()).ToDictionary(f => f.Id);
        var ids = dto.FacilityIds.Distinct().ToList();
        if (ids.Count == 0) throw new ServiceException("Pick at least one facility.");

        var byDept = new Dictionary<DepartmentType, DepartmentRequest>();
        var names = new List<string>();
        foreach (var id in ids)
        {
            if (!allFacilities.TryGetValue(id, out var f)) throw new ServiceException("Selected facility does not exist.");
            if (f.IsOther) throw new ServiceException("Pick a listed facility (not 'Others') for institutional events.");

            if (!byDept.TryGetValue(f.Department, out var dr))
            {
                dr = new DepartmentRequest
                {
                    Department = f.Department,
                    Status = EventStatus.Approved,
                    ReviewedBy = adminEmail,
                    ReviewedAt = DateTime.Now
                };
                byDept[f.Department] = dr;
            }
            dr.Facilities.Add(new EventFacility { FacilityId = f.Id });
            names.Add(f.Name);
        }

        await ResolveConflictsAsync(ids, dto.StartTime, dto.EndTime, false, null);

        var ev = new CampusEvent
        {
            Title = dto.Title.Trim(),
            RequesterName = adminName,
            RequesterEmail = adminEmail,
            Organization = dto.Organization.Trim(),
            Role = RequesterRole.Administrator,
            TargetAudience = dto.TargetAudience,
            Purpose = dto.Purpose.Trim(),
            Capacity = 0,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            IsInstitutional = true,
            RequiresRegistration = false,
            ManageCode = NewManageCode(),
            Status = EventStatus.Approved,
            DepartmentRequests = byDept.Values.ToList()
        };
        await _events.AddAsync(ev);
        return new EventCreatedDto(Summary(ev, ids, names), ev.ManageCode);
    }

    // ------------------------------------------------------- requester side

    public async Task<List<EventDetailDto>> GetMineAsync(string email) =>
        (await _events.GetMineAsync(email.ToLowerInvariant())).Select(e => e.ToDetail()).ToList();

    public async Task<EventDetailDto> GetDetailAsync(int id) =>
        (await _events.GetDetailAsync(id) ?? throw new ServiceException("Event not found.", 404)).ToDetail();

    public async Task<EventDetailDto> UpdateAsync(int id, EventUpdateDto dto, string email)
    {
        var ev = await LoadOwnedAsync(id, email);
        if (ev.Status != EventStatus.Pending)
            throw new ServiceException("Only pending requests can be edited. You can still move or cancel the event.");

        var full = await _events.GetDetailAsync(id) ?? throw new ServiceException("Event not found.", 404);
        var registered = full.Registrations.Count(r => !r.Cancelled);
        if (dto.Capacity < registered)
            throw new ServiceException($"{registered} people already registered, so the number cannot go lower than that.");
        foreach (var ef in full.DepartmentRequests.SelectMany(d => d.Facilities))
        {
            var f = ef.Facility!;
            if (!f.IsOther && f.Capacity.HasValue && f.Capacity.Value < dto.Capacity)
                throw new ServiceException($"{f.Name} only fits {f.Capacity} people.");
        }

        ev.Title = dto.Title.Trim();
        ev.Purpose = dto.Purpose.Trim();
        ev.Capacity = dto.Capacity;
        await _events.SaveAsync();
        return await GetDetailAsync(id);
    }

    public async Task<EventDetailDto> RescheduleAsync(int id, RescheduleDto dto, string email)
    {
        var ev = await LoadOwnedAsync(id, email);
        await RescheduleCoreAsync(ev, dto);
        return await GetDetailAsync(id);
    }

    // Soft delete: the event stays in the database with status Cancelled
    public async Task CancelAsync(int id, string email) =>
        await CancelCoreAsync(await LoadOwnedAsync(id, email));

    // ---------------------------------------------------------------- guests

    public async Task<EventDetailDto> GetGuestEventAsync(int id, string email, string code)
    {
        await LoadGuestAsync(id, email, code);
        return await GetDetailAsync(id);
    }

    public async Task<EventDetailDto> RescheduleGuestAsync(int id, RescheduleDto dto, string email, string code)
    {
        await RescheduleCoreAsync(await LoadGuestAsync(id, email, code), dto);
        return await GetDetailAsync(id);
    }

    public async Task CancelGuestAsync(int id, string email, string code) =>
        await CancelCoreAsync(await LoadGuestAsync(id, email, code));

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

    public async Task ApproveAsync(int id, DepartmentType? dept, string adminEmail)
    {
        var dr = await LoadForReviewAsync(id, dept);

        var facilityIds = dr.Facilities.Where(f => f.Facility is { IsOther: false }).Select(f => f.FacilityId).ToList();
        if (facilityIds.Count > 0)
        {
            var taken = await _events.GetConflictsAsync(
                facilityIds, dr.CampusEvent!.StartTime, dr.CampusEvent.EndTime,
                approvedOnly: true, excludeEventId: dr.CampusEventId);
            if (taken.Count > 0)
                throw new ServiceException(
                    $"Another approved event already uses: {string.Join(", ", taken.Select(t => t.FacilityName).Distinct())}.", 409);
        }

        dr.Status = EventStatus.Approved;
        dr.ReviewedBy = adminEmail;
        dr.ReviewedAt = DateTime.Now;
        dr.RejectionReason = null;
        RecomputeStatus(dr.CampusEvent!);
        await _deptRequests.SaveAsync();
    }

    public async Task RejectAsync(int id, DepartmentType? dept, string adminEmail, string reason)
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
            other.RejectionReason = $"Closed because {FacilityMapper.OfficeName(dr.Department)} rejected the event.";
        }
        RecomputeStatus(dr.CampusEvent);
        await _deptRequests.SaveAsync();
    }

    // Admins can cancel (soft delete) an event. An office admin only for events that involve their office.
    public async Task AdminCancelEventAsync(int eventId, DepartmentType? dept)
    {
        var ev = await _events.GetByIdAsync(eventId) ?? throw new ServiceException("Event not found.", 404);
        if (dept.HasValue && !ev.DepartmentRequests.Any(d => d.Department == dept.Value))
            throw new ServiceException("This event does not involve your office.", 403);
        if (ev.Status == EventStatus.Cancelled) throw new ServiceException("This event is already cancelled.");
        ev.Status = EventStatus.Cancelled;
        await _events.SaveAsync();
    }

    public async Task<(string PhysicalPath, string ContentType, string DownloadName)> GetEndorsementAsync(int id, DepartmentType? dept)
    {
        var dr = await _deptRequests.GetByIdAsync(id) ?? throw new ServiceException("Request not found.", 404);
        if (dept.HasValue && dr.Department != dept) throw new ServiceException("Not allowed.", 403);
        if (dr.EndorsementStoredName == null) throw new ServiceException("No endorsement letter was uploaded.", 404);

        var path = Path.Combine(UploadDir, dr.EndorsementStoredName);
        if (!File.Exists(path)) throw new ServiceException("File is missing on the server.", 404);

        var ext = Path.GetExtension(dr.EndorsementStoredName);
        return (path, AllowedFiles.GetValueOrDefault(ext, "application/octet-stream"),
                dr.EndorsementOriginalName ?? dr.EndorsementStoredName);
    }

    // --------------------------------------------------------------- helpers

    private static string NewManageCode() => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    private static EventSummaryDto Summary(CampusEvent ev, List<int> facilityIds, List<string> names) => new(
        ev.Id, ev.Title, ev.Organization, ev.Purpose,
        facilityIds.Distinct().ToList(), names.Distinct().ToList(),
        ev.StartTime, ev.EndTime, ev.TargetAudience, ev.Capacity, 0,
        ev.IsInstitutional, ev.RequiresRegistration, ev.Status);

    // Outsiders must use the guest form; school emails have to sign in
    private void RejectSchoolEmail(string email)
    {
        var domain = _config["Auth:AllowedEmailDomain"];
        if (!string.IsNullOrWhiteSpace(domain) &&
            email.Trim().EndsWith("@" + domain.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ServiceException("School accounts must sign in. The guest form is for outsiders only.");
    }

    private static bool IsWeekendRange(DateTime start, DateTime end)
    {
        for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
            if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) return false;
        return true;
    }

    private static void ValidateTimes(DateTime start, DateTime end, bool isGuest)
    {
        if (end <= start) throw new ServiceException("End time must be after the start time.");
        if (start < DateTime.Now) throw new ServiceException("The event must start in the future.");
        if (isGuest && !IsWeekendRange(start, end))
            throw new ServiceException("Outsiders can only book on weekends (Saturday and Sunday).");
    }

    // Same facility + overlapping time against pending OR approved events = conflict (409).
    // Exception: a school event may take the place of a PENDING guest request (school has priority).
    private async Task ResolveConflictsAsync(List<int> facilityIds, DateTime start, DateTime end, bool requesterIsGuest, int? excludeEventId)
    {
        var conflicts = await _events.GetConflictsAsync(facilityIds, start, end, approvedOnly: false, excludeEventId);
        if (conflicts.Count == 0) return;

        if (requesterIsGuest || conflicts.Any(c => !c.PendingGuest))
            throw new ServiceException(
                $"Already booked or pending for this time: {string.Join(", ", conflicts.Select(c => c.FacilityName).Distinct())}.", 409);

        foreach (var eventId in conflicts.Select(c => c.EventId).Distinct())
        {
            var guestEvent = await _events.GetByIdAsync(eventId);
            if (guestEvent == null) continue;
            foreach (var dr in guestEvent.DepartmentRequests.Where(d => d.Status == EventStatus.Pending))
            {
                dr.Status = EventStatus.Rejected;
                dr.ReviewedBy = "system";
                dr.ReviewedAt = DateTime.Now;
                dr.RejectionReason = "A school event needs this facility at the same time (school events have priority).";
            }
            guestEvent.Status = EventStatus.Rejected;
        }
        await _events.SaveAsync();
    }

    private async Task RescheduleCoreAsync(CampusEvent ev, RescheduleDto dto)
    {
        if (ev.Status is not (EventStatus.Pending or EventStatus.Approved))
            throw new ServiceException("Only pending or approved events can be moved.");
        ValidateTimes(dto.StartTime, dto.EndTime, ev.IsGuest);

        var facilityIds = ev.DepartmentRequests.SelectMany(d => d.Facilities)
            .Where(f => f.Facility is { IsOther: false }).Select(f => f.FacilityId).Distinct().ToList();
        await ResolveConflictsAsync(facilityIds, dto.StartTime, dto.EndTime, ev.IsGuest, ev.Id);

        ev.StartTime = dto.StartTime;
        ev.EndTime = dto.EndTime;

        // A new date has to be approved again by every office (institutional events stay approved)
        if (!ev.IsInstitutional)
        {
            foreach (var dr in ev.DepartmentRequests.Where(d => d.Status == EventStatus.Approved))
            {
                dr.Status = EventStatus.Pending;
                dr.ReviewedBy = null;
                dr.ReviewedAt = null;
            }
            RecomputeStatus(ev);
        }
        await _events.SaveAsync();
    }

    private async Task CancelCoreAsync(CampusEvent ev)
    {
        if (ev.Status is EventStatus.Cancelled or EventStatus.Rejected)
            throw new ServiceException("This event is already closed.");
        if (ev.StartTime < DateTime.Now) throw new ServiceException("The event already started.");
        ev.Status = EventStatus.Cancelled;
        await _events.SaveAsync();
    }

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

    private async Task<CampusEvent> LoadGuestAsync(int id, string email, string code)
    {
        var ev = await _events.GetByIdAsync(id) ?? throw new ServiceException("Event not found.", 404);
        if (!ev.IsGuest
            || !string.Equals(ev.RequesterEmail, email.Trim(), StringComparison.OrdinalIgnoreCase)
            || !string.Equals(ev.ManageCode, code.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ServiceException("The email or manage code is wrong.", 403);
        return ev;
    }

    private async Task<DepartmentRequest> LoadForReviewAsync(int id, DepartmentType? dept)
    {
        var dr = await _deptRequests.GetByIdAsync(id) ?? throw new ServiceException("Request not found.", 404);
        if (dept.HasValue && dr.Department != dept) throw new ServiceException("This request belongs to another office.", 403);
        if (dr.CampusEvent!.Status == EventStatus.Cancelled) throw new ServiceException("This event was cancelled.");
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
