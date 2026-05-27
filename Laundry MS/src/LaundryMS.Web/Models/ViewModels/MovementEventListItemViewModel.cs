namespace LaundryMS.Web.Models.ViewModels;

public class MovementEventListItemViewModel
{
    public ulong Id { get; init; }
    public string RfidTag { get; init; } = string.Empty;
    public ulong LinenItemId { get; init; }
    public string ReaderName { get; init; } = string.Empty;
    public ulong ReaderId { get; init; }
    public string WayName { get; init; } = string.Empty;
    public ulong ReaderWayId { get; init; }
    public string ProcessingResult { get; init; } = string.Empty;
    public ulong? LogisticsJobId { get; init; }
    public string? RejectionReason { get; init; }
    public string? ConditionAfterEvent { get; init; }
    public string? IdempotencyKey { get; init; }
    public DateTime OccurredAt { get; init; }
    public DateTime ReceivedAtServer { get; init; }

    public int IngestLagSeconds { get; init; }
}
