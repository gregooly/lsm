namespace LaundryMS.Web.Models.ViewModels;

public class LinenItemTimelineEventViewModel
{
    public ulong EventId { get; init; }
    public DateTime OccurredAt { get; init; }
    public string ReaderName { get; init; } = string.Empty;
    public string ScanRouteName { get; init; } = string.Empty;
    public string ProcessingResult { get; init; } = string.Empty;
    public string? ConditionAfterEvent { get; init; }
    public string? RejectionReason { get; init; }
    public ulong? LogisticsJobId { get; init; }
    public string? LogisticsJobType { get; init; }
}
