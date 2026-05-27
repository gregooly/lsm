namespace LaundryMS.Web.Models.ViewModels;

public class CollectionJobDetailViewModel
{
    public ulong Id { get; init; }
    public string JobType { get; init; } = string.Empty;
    public string JobStatus { get; init; } = string.Empty;
    public string? CustomerName { get; init; }
    public string? DriverName { get; init; }
    public string? FromLocationName { get; init; }
    public string? ToLocationName { get; init; }
    public string? ReaderWayName { get; init; }
    public string? ReaderNameOnWay { get; init; }
    public string? Notes { get; init; }
    public DateTime? PlannedStartAt { get; init; }
    public DateTime? PlannedEndAt { get; init; }
    public DateTime? ActualStartAt { get; init; }
    public DateTime? ActualEndAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public int ExpectedItemCount { get; init; }
    public int ReachedItemCount { get; init; }
    public int ScannedUniqueTagCount { get; init; }
    public DateTime? LastScanAt { get; init; }

    public IReadOnlyList<CollectionExpectedItemRowViewModel> ExpectedItems { get; init; } = [];
    public IReadOnlyList<CollectionScanEventRowViewModel> RecentScans { get; init; } = [];
}

public class CollectionExpectedItemRowViewModel
{
    public ulong JobExpectedItemId { get; init; }
    public string RfidTag { get; init; } = string.Empty;
    public string ItemType { get; init; } = string.Empty;
    public string? SizeLabel { get; init; }
    public string? PhysicalCondition { get; init; }
    public string ExpectedProcessStatus { get; init; } = string.Empty;
    public bool ReachedExpectedStatus { get; init; }
    public DateTime? ReachedAt { get; init; }
}

public class CollectionScanEventRowViewModel
{
    public ulong EventId { get; init; }
    public string RfidTag { get; init; } = string.Empty;
    public string ReaderName { get; init; } = string.Empty;
    public string WayName { get; init; } = string.Empty;
    public string ProcessingResult { get; init; } = string.Empty;
    public string? ConditionAfterEvent { get; init; }
    public DateTime OccurredAt { get; init; }
}
