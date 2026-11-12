using EventManagement.Mapper;
using EventManagement.Model.Domain;
using EventManagement.Model.DTO;
using EventManagement.Repository;

namespace EventManagement.Service;

public class EquipmentService : IEquipmentService
{
    private readonly IEquipmentRepository _equipment;
    private readonly IEventEquipmentRepository _items;

    public EquipmentService(IEquipmentRepository equipment, IEventEquipmentRepository items)
    {
        _equipment = equipment;
        _items = items;
    }

    public async Task<List<EquipmentDto>> GetCatalogAsync() =>
        (await _equipment.GetAllAsync()).Select(e => e.ToDto()).ToList();

    public async Task<List<AdminEquipmentRequestDto>> GetRequestsForAdminAsync(DepartmentType dept, EventStatus status) =>
        (await _items.GetByDepartmentAsync(dept, status)).Select(x => x.ToAdminDto()).ToList();

    public async Task ApproveAsync(int id, DepartmentType dept, string adminEmail)
    {
        var item = await LoadForReviewAsync(id, dept);
        var request = item.DepartmentRequest!;
        request.Status = EventStatus.Approved;
        request.ReviewedBy = adminEmail;
        request.ReviewedAt = DateTime.Now;
        request.RejectionReason = null;
        request.CampusEvent?.RecomputeStatus();
        await _items.SaveAsync();
    }

    public async Task RejectAsync(int id, DepartmentType dept, string adminEmail, string reason)
    {
        var item = await LoadForReviewAsync(id, dept);
        var request = item.DepartmentRequest!;
        request.Status = EventStatus.Rejected;
        request.ReviewedBy = adminEmail;
        request.ReviewedAt = DateTime.Now;
        request.RejectionReason = reason.Trim();
        request.CampusEvent?.RecomputeStatus();
        await _items.SaveAsync();
    }

    private async Task<EventEquipment> LoadForReviewAsync(int id, DepartmentType dept)
    {
        var item = await _items.GetByIdAsync(id) ?? throw new ServiceException("Equipment request not found.", 404);
        var request = item.DepartmentRequest;
        if (item.Equipment!.Department != dept || request?.Department != dept)
            throw new ServiceException("This equipment belongs to another office.", 403);
        if (request.Status != EventStatus.Pending)
            throw new ServiceException("This equipment request was already reviewed.");
        if (request.CampusEvent == null || request.CampusEvent.Status == EventStatus.Rejected)
            throw new ServiceException("The event request was rejected.");
        return item;
    }
}