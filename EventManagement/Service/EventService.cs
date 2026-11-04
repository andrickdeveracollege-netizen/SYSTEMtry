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

        var ev = new CampusEvent
        {
            Title = dto.Title.Trim(),
            RequesterName = dto.RequesterName.Trim(),
            RequesterEmail = requesterEmail,
            Organization = dto.Organization.Trim(),
            Role = dto.Role,
            TargetAudience = dto.TargetAudience,
            Purpose = dto.Purpose.Trim(),
            FacilityId = facility.Id,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Status = EventStatus.Pending,
            ApproverDepartment = facility.Department,     // approval routing
            EndorsementStoredName = storedName,
            EndorsementOriginalName = originalName,
            EquipmentItems = items
        };
        await _events.AddAsync(ev);
        ev.Facility = facility;
        return ev.ToSummary();
    }

    public async Task<List<AdminRequestDto>> GetRequestsForAdminAsync(DepartmentType dept, EventStatus status) =>
        (await _events.GetByDepartmentAsync(dept, status)).Select(e => e.ToAdminDto()).ToList();

    public async Task ApproveAsync(int id, DepartmentType dept, string adminEmail)
    {
        var ev = await LoadForReviewAsync(id, dept);
        if (await _events.HasConflictAsync(ev.FacilityId, ev.StartTime, ev.EndTime, approvedOnly: true, excludeId: ev.Id))
            throw new ServiceException("Another approved event already uses this facility at that time.", 409);

        ev.Status = EventStatus.Approved;
        ev.ReviewedBy = adminEmail;
        ev.ReviewedAt = DateTime.Now;
        ev.RejectionReason = null;
        await _events.SaveAsync();
    }

    public async Task RejectAsync(int id, DepartmentType dept, string adminEmail, string reason)
    {
        var ev = await LoadForReviewAsync(id, dept);
        ev.Status = EventStatus.Rejected;
        ev.ReviewedBy = adminEmail;
        ev.ReviewedAt = DateTime.Now;
        ev.RejectionReason = reason.Trim();

        // A rejected event no longer needs its pending equipment
        foreach (var item in ev.EquipmentItems.Where(i => i.Status == EventStatus.Pending))
        {
            item.Status = EventStatus.Rejected;
            item.ReviewedBy = adminEmail;
            item.ReviewedAt = DateTime.Now;
            item.RejectionReason = "The event request was rejected.";
        }
        await _events.SaveAsync();
    }

    public async Task<(string PhysicalPath, string ContentType, string DownloadName)> GetEndorsementAsync(int id, DepartmentType dept)
    {
        var ev = await _events.GetByIdAsync(id) ?? throw new ServiceException("Request not found.", 404);
        if (ev.ApproverDepartment != dept) throw new ServiceException("Not allowed.", 403);
        if (ev.EndorsementStoredName == null) throw new ServiceException("No endorsement letter was uploaded.", 404);

        var path = Path.Combine(UploadDir, ev.EndorsementStoredName);
        if (!File.Exists(path)) throw new ServiceException("File is missing on the server.", 404);

        var ext = Path.GetExtension(ev.EndorsementStoredName);
        return (path, AllowedFiles.GetValueOrDefault(ext, "application/octet-stream"),
                ev.EndorsementOriginalName ?? ev.EndorsementStoredName);
    }

    private async Task<List<EventEquipment>> BuildEquipmentItemsAsync(List<EquipmentRequestItemDto> requested)
    {
        var items = new List<EventEquipment>();
        if (requested.Count == 0) return items;

        var catalog = (await _equipment.GetAllAsync()).ToDictionary(x => x.Id);
        foreach (var req in requested)
        {
            if (!catalog.TryGetValue(req.EquipmentId, out var eq))
                throw new ServiceException("Selected equipment does not exist.");
            if (items.Any(i => i.EquipmentId == eq.Id))
                throw new ServiceException($"{eq.Name} was selected more than once.");
            if (req.Quantity < 1 || req.Quantity > 100)
                throw new ServiceException($"Quantity for {eq.Name} must be between 1 and 100.");
            if (eq.IsOther && string.IsNullOrWhiteSpace(req.Notes))
                throw new ServiceException(
                    $"Please describe the 'Others' equipment for {FacilityMapper.OfficeName(eq.Department)}.");

            items.Add(new EventEquipment
            {
                EquipmentId = eq.Id,
                Quantity = req.Quantity,
                Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim()
            });
        }
        return items;
    }

    private async Task<CampusEvent> LoadForReviewAsync(int id, DepartmentType dept)
    {
        var ev = await _events.GetByIdAsync(id) ?? throw new ServiceException("Request not found.", 404);
        if (ev.ApproverDepartment != dept) throw new ServiceException("This request belongs to another office.", 403);
        if (ev.Status != EventStatus.Pending) throw new ServiceException("This request was already reviewed.");
        return ev;
    }

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