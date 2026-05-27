namespace LaundryMS.Web.Models.ViewModels;

public class LogisticsJobListItemViewModel
{
    public ulong Id { get; init; }
    public string JobType { get; init; } = string.Empty;
    public string JobStatus { get; init; } = string.Empty;
    public string? CustomerName { get; init; }
    public string? DriverName { get; init; }
    public ulong? ReaderWayId { get; init; }
    public string? ReaderWayName { get; init; }
    public string? FromLocationName { get; init; }
    public string? ToLocationName { get; init; }
    public DateTime? LastScanAt { get; init; }
    public int ScannedUniqueTagCount { get; init; }
    public DateTime? PlannedStartAt { get; init; }
    public DateTime? PlannedEndAt { get; init; }
    public DateTime? ActualStartAt { get; init; }
    public DateTime? ActualEndAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int ExpectedItemCount { get; init; }
    public int ReachedItemCount { get; init; }
    public int? Delta { get; init; }
}
