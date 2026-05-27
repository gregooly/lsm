namespace LaundryMS.Web.Models.ViewModels;

public class LinenItemDetailViewModel
{
    public ulong Id { get; init; }
    public string RfidTag { get; init; } = string.Empty;
    public string ItemType { get; init; } = string.Empty;
    public string? SizeLabel { get; init; }
    public string DefaultAssignmentType { get; init; } = string.Empty;
    public ulong? OwnerCustomerId { get; init; }
    public string? OwnerCustomerName { get; init; }
    public ulong? AssignedEmployeeId { get; init; }
    public string? AssignedEmployeeName { get; init; }
    public ulong? CurrentLocationId { get; init; }
    public string? CurrentLocationName { get; init; }
    public string CurrentProcessStatus { get; init; } = string.Empty;
    public string PhysicalCondition { get; init; } = string.Empty;
    public DateTime? LastScannedAt { get; init; }
    public string LifecycleState { get; init; } = "active";
    public string? DeactivationReason { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int TotalScanCount { get; init; }
    public int RejectedScanCount { get; init; }
    public DateTime? LastAcceptedScanAt { get; init; }
    public int? DaysSinceLastScan { get; init; }

    public IReadOnlyList<LinenItemTimelineEventViewModel> Timeline { get; init; } = [];
}
