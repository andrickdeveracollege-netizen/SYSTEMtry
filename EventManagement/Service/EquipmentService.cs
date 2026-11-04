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
        item.Status = EventStatus.Approved;
        item.ReviewedBy = adminEmail;
        item.ReviewedAt = DateTime.Now;
        item.RejectionReason = null;
        await _items.SaveAsync();
    }

    public async Task RejectAsync(int id, DepartmentType dept, string adminEmail, string reason)
    {
        var item = await LoadForReviewAsync(id, dept);
        item.Status = EventStatus.Rejected;
        item.ReviewedBy = adminEmail;
        item.ReviewedAt = DateTime.Now;
        item.RejectionReason = reason.Trim();
        await _items.SaveAsync();
    }

    private async Task<EventEquipment> LoadForReviewAsync(int id, DepartmentType dept)
    {
        var item = await _items.GetByIdAsync(id) ?? throw new ServiceException("Equipment request not found.", 404);
        if (item.Equipment!.Department != dept)
            throw new ServiceException("This equipment belongs to another office.", 403);
        if (item.Status != EventStatus.Pending)
            throw new ServiceException("This equipment request was already reviewed.");
        if (item.CampusEvent!.Status == EventStatus.Rejected)
            throw new ServiceException("The event request was rejected.");
        return item;
    }
}