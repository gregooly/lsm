namespace LaundryMS.Web.Models.ViewModels;

public class ScanHistoryQueryViewModel
{
    public string? Tag { get; init; }
    public string? Q { get; init; }
    public ulong? ReaderId { get; init; }
    public ulong? ReaderWayId { get; init; }
    public string? ProcessingResult { get; init; }
    public ulong? JobId { get; init; }
    public string? RejectionReason { get; init; }
    public string? IdempotencyKey { get; init; }
    public bool OnlyRejected { get; init; }
    public bool OnlyDuplicates { get; init; }
    public int MinIngestLagSeconds { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string SortBy { get; init; } = "occurred";
    public string SortDir { get; init; } = "desc";
}
