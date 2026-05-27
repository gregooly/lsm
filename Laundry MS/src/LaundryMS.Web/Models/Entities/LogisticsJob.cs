namespace LaundryMS.Web.Models.Entities;

public class LogisticsJob
{
    public ulong Id { get; set; }
    public string JobType { get; set; } = string.Empty;
    /// <summary>External tenant id (JWT scope).</summary>
    public ulong? CustomerId { get; set; }
    public ulong? DriverId { get; set; }
    public ulong? FromLocationId { get; set; }
    public ulong? ToLocationId { get; set; }
    public ulong? ReaderWayId { get; set; }
    public string JobStatus { get; set; } = "open";
    public DateTime? PlannedStartAt { get; set; }
    public DateTime? PlannedEndAt { get; set; }
    public DateTime? ActualStartAt { get; set; }
    public DateTime? ActualEndAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Driver? Driver { get; set; }
    public Location? FromLocation { get; set; }
    public Location? ToLocation { get; set; }
    public ReaderWay? ReaderWay { get; set; }
    public ICollection<JobExpectedItem> ExpectedItems { get; set; } = new List<JobExpectedItem>();
}
