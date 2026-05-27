namespace LaundryMS.Web.Models.ViewModels;

public class LinenItemHubRowViewModel
{
    public ulong Id { get; init; }
    public string RfidTag { get; init; } = string.Empty;
    public string ItemType { get; init; } = string.Empty;
    public string? SizeLabel { get; init; }
    public string DefaultAssignmentType { get; init; } = string.Empty;
    public string CurrentProcessStatus { get; init; } = string.Empty;
    public string PhysicalCondition { get; init; } = string.Empty;
    public DateTime? LastScannedAt { get; init; }
    public int? DaysSinceLastScan { get; init; }
    public int RecentRejectedCount { get; init; }
    public string LifecycleState { get; init; } = "active";
    public bool IsActive { get; init; }
    public string? OwnerCustomerName { get; init; }
    public string? AssignedEmployeeName { get; init; }
    public string? CurrentLocationName { get; init; }
}
