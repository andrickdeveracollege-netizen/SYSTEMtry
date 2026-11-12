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
    private readonly IFacilityRepository _facilities;
    private readonly IEquipmentRepository _equipment;
    private readonly IWebHostEnvironment _env;

    public EventService(
        IEventRepository events,
        IFacilityRepository facilities,
        IEquipmentRepository equipment,
        IWebHostEnvironment env)
    {
        _events = events;
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

    public async Task<EventSummaryDto> CreateRequestAsync(EventRequestDto dto, string requesterEmail)
    {
        if (!Enum.IsDefined(dto.Role)) throw new ServiceException("Please select your role (Faculty or Student).");
        if (dto.TargetAudience == YearLevel.None || ((int)dto.TargetAudience & ~(int)YearLevel.All) != 0)
            throw new ServiceException("Please select at least one target audience.");
        if (dto.EndTime <= dto.StartTime) throw new ServiceException("End time must be after the start time.");
        if (dto.StartTime < DateTime.Now) throw new ServiceException("The event must start in the future.");

        var facility = await _facilities.GetByIdAsync(dto.FacilityId)
                       ?? throw new ServiceException("Selected facility does not exist.");
        bool isOtherFacility = string.Equals(facility.Name, "Others", StringComparison.OrdinalIgnoreCase);
        string? otherFacilityName = dto.OtherFacilityName?.Trim();
        if (isOtherFacility && string.IsNullOrWhiteSpace(otherFacilityName))
            throw new ServiceException("Please specify the facility name for 'Others'.");
        if (!isOtherFacility) otherFacilityName = null;

        // Double-booking guard: same facility + overlapping time against pending OR approved events
        if (await _events.HasConflictAsync(facility.Id, dto.StartTime, dto.EndTime, approvedOnly: false))
            throw new ServiceException($"{facility.Name} is already booked or has a pending request that overlaps this time.", 409);

        // Equipment (optional). Validate before saving any file.
        var items = await BuildEquipmentItemsAsync(dto.Equipment);

        // Endorsement letter: required except for College (Ms. Judith) facilities
        bool optional = facility.Department == DepartmentType.College;
        string? storedName = null, originalName = null;
        if (dto.Endorsement is { Length: > 0 })
        {
            (storedName, originalName) = await SaveFileAsync(dto.Endorsement);
        }
        else if (!optional)
        {
            throw new ServiceException("An endorsement letter (image, PDF or DOCX) is required for this facility.");
        }

        var requestsByDepartment = items.GroupBy(item => item.Department)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Item).ToList());
        var departments = requestsByDepartment.Keys.Append(facility.Department).Distinct();
        var departmentRequests = new List<DepartmentRequest>();
        foreach (var department in departments)
        {
            var request = new DepartmentRequest
            {
                Department = department,
                EndorsementStoredName = storedName,
                EndorsementOriginalName = originalName
            };
            if (department == facility.Department)
                request.Facilities.Add(new EventFacility { FacilityId = facility.Id, OtherName = otherFacilityName });
            if (requestsByDepartment.TryGetValue(department, out var departmentItems))
                foreach (var item in departmentItems) request.EquipmentItems.Add(item);
            departmentRequests.Add(request);
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
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Status = EventStatus.Pending,
            DepartmentRequests = departmentRequests
        };
        await _events.AddAsync(ev);
        return ev.ToSummary();
    }

    public async Task<List<AdminRequestDto>> GetRequestsForAdminAsync(DepartmentType dept, EventStatus status) =>
        (await _events.GetByDepartmentAsync(dept, status)).Select(e => e.ToAdminDto(dept)).ToList();

    public async Task ApproveAsync(int id, DepartmentType dept, string adminEmail)
    {
        var ev = await LoadForReviewAsync(id, dept);
        var request = GetDepartmentRequest(ev, dept);
        foreach (var facility in request.Facilities)
            if (await _events.HasConflictAsync(facility.FacilityId, ev.StartTime, ev.EndTime, approvedOnly: true, excludeId: ev.Id))
                throw new ServiceException("Another approved event already uses this facility at that time.", 409);

        request.Status = EventStatus.Approved;
        request.ReviewedBy = adminEmail;
        request.ReviewedAt = DateTime.Now;
        request.RejectionReason = null;
        ev.RecomputeStatus();
        await _events.SaveAsync();
    }

    public async Task RejectAsync(int id, DepartmentType dept, string adminEmail, string reason)
    {
        var ev = await LoadForReviewAsync(id, dept);
        var request = GetDepartmentRequest(ev, dept);
        request.Status = EventStatus.Rejected;
        request.ReviewedBy = adminEmail;
        request.ReviewedAt = DateTime.Now;
        request.RejectionReason = reason.Trim();
        ev.RecomputeStatus();
        await _events.SaveAsync();
    }

    public async Task<(string PhysicalPath, string ContentType, string DownloadName)> GetEndorsementAsync(int id, DepartmentType dept)
    {
        var ev = await _events.GetByIdAsync(id) ?? throw new ServiceException("Request not found.", 404);
        var request = ev.DepartmentRequests.FirstOrDefault(r => r.Department == dept)
                      ?? throw new ServiceException("Not allowed.", 403);
        if (request.EndorsementStoredName == null) throw new ServiceException("No endorsement letter was uploaded.", 404);

        var path = Path.Combine(UploadDir, request.EndorsementStoredName);
        if (!File.Exists(path)) throw new ServiceException("File is missing on the server.", 404);

        var ext = Path.GetExtension(request.EndorsementStoredName);
        return (path, AllowedFiles.GetValueOrDefault(ext, "application/octet-stream"),
                request.EndorsementOriginalName ?? request.EndorsementStoredName);
    }

    private async Task<List<(EventEquipment Item, DepartmentType Department)>> BuildEquipmentItemsAsync(List<EquipmentRequestItemDto> requested)
    {
        var items = new List<(EventEquipment Item, DepartmentType Department)>();
        if (requested.Count == 0) return items;

        var catalog = (await _equipment.GetAllAsync()).ToDictionary(x => x.Id);
        foreach (var req in requested)
        {
            if (!catalog.TryGetValue(req.EquipmentId, out var eq))
                throw new ServiceException("Selected equipment does not exist.");
            if (items.Any(i => i.Item.EquipmentId == eq.Id))
                throw new ServiceException($"{eq.Name} was selected more than once.");
            if (req.Quantity < 1 || req.Quantity > 100)
                throw new ServiceException($"Quantity for {eq.Name} must be between 1 and 100.");
            if (eq.IsOther && string.IsNullOrWhiteSpace(req.Notes))
                throw new ServiceException(
                    $"Please describe the 'Others' equipment for {FacilityMapper.OfficeName(eq.Department)}.");

            items.Add((new EventEquipment
            {
                EquipmentId = eq.Id,
                Quantity = req.Quantity,
                Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim()
            }, eq.Department));
        }
        return items;
    }

    private async Task<CampusEvent> LoadForReviewAsync(int id, DepartmentType dept)
    {
        var ev = await _events.GetByIdAsync(id) ?? throw new ServiceException("Request not found.", 404);
        var request = ev.DepartmentRequests.FirstOrDefault(r => r.Department == dept);
        if (request == null) throw new ServiceException("This request belongs to another office.", 403);
        if (request.Status != EventStatus.Pending) throw new ServiceException("This request was already reviewed.");
        return ev;
    }

    private static DepartmentRequest GetDepartmentRequest(CampusEvent ev, DepartmentType dept) =>
        ev.DepartmentRequests.First(request => request.Department == dept);

    private async Task<(string stored, string original)> SaveFileAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName);
        if (!AllowedFiles.ContainsKey(ext))
            throw new ServiceException("Endorsement letter must be an image (JPG/PNG), PDF or DOCX file.");
        if (file.Length > MaxFileBytes)
            throw new ServiceException("Endorsement letter must be 5 MB or smaller.");

        Directory.CreateDirectory(UploadDir);
        var stored = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        await using var fs = File.Create(Path.Combine(UploadDir, stored));
        await file.CopyToAsync(fs);
        return (stored, Path.GetFileName(file.FileName));
    }
}