namespace LaundryMS.Web.Models.ViewModels;

public class LinenItemListItemViewModel
{
    public ulong Id { get; init; }
    public string RfidTag { get; init; } = string.Empty;
    public string ItemType { get; init; } = string.Empty;
    public string? SizeLabel { get; init; }
    public string DefaultAssignmentType { get; init; } = string.Empty;
    public ulong? OwnerCustomerId { get; init; }
    public ulong? AssignedEmployeeId { get; init; }
    public ulong? CurrentLocationId { get; init; }
    public string CurrentProcessStatus { get; init; } = string.Empty;
    public string PhysicalCondition { get; init; } = string.Empty;
    public string? OwnerCustomerName { get; init; }
    public string? AssignedEmployeeName { get; init; }
    public string? CurrentLocationName { get; init; }
    public bool IsActive { get; init; }
    public string LifecycleState { get; init; } = "active";
    public DateTime? LastScannedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
