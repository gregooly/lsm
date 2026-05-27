namespace LaundryMS.Web.Models.Entities;

public class LinenMovementEvent
{
    public ulong Id { get; set; }
    public ulong? CustomerId { get; set; }
    public ulong LinenItemId { get; set; }
    public ulong ReaderId { get; set; }
    public ulong ReaderWayId { get; set; }
    public ulong? LogisticsJobId { get; set; }
    public ulong? DriverId { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime ReceivedAtServer { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string ProcessingResult { get; set; } = "applied";
    public string? RejectionReason { get; set; }
    public string? ConditionAfterEvent { get; set; }
    public DateTime CreatedAt { get; set; }

    public LinenItem LinenItem { get; set; } = null!;
    public Reader Reader { get; set; } = null!;
    public ReaderWay ReaderWay { get; set; } = null!;
    public LogisticsJob? LogisticsJob { get; set; }
    public Driver? Driver { get; set; }
}
