namespace LaundryMS.Web.Models.ViewModels;

public class LogisticsJobDetailViewModel
{
    public ulong Id { get; init; }
    public string JobType { get; init; } = string.Empty;
    public string JobStatus { get; init; } = string.Empty;
    public string? CustomerName { get; init; }
    public string? DriverName { get; init; }
    public string? FromLocationName { get; init; }
    public string? ToLocationName { get; init; }
    public ulong? ReaderWayId { get; init; }
    public string? ReaderWayName { get; init; }
    public DateTime? PlannedStartAt { get; init; }
    public DateTime? PlannedEndAt { get; init; }
    public DateTime? ActualStartAt { get; init; }
    public DateTime? ActualEndAt { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public DateTime? LastScanAt { get; init; }
    public int ExpectedItemCount { get; init; }
    public int ReachedItemCount { get; init; }
    public int ScannedUniqueTagCount { get; init; }
    public int Delta => ReachedItemCount - ExpectedItemCount;

    public IReadOnlyList<LogisticsJobScanRowViewModel> RecentScans { get; init; } = [];
}

public class LogisticsJobScanRowViewModel
{
    public ulong EventId { get; init; }
    public string RfidTag { get; init; } = string.Empty;
    public string ReaderName { get; init; } = string.Empty;
    public string ReaderWayName { get; init; } = string.Empty;
    public string ProcessingResult { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
}
