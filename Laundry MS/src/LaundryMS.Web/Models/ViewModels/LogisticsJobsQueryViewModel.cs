namespace LaundryMS.Web.Models.ViewModels;

public class LogisticsJobsQueryViewModel
{
    /// <summary>all | collection | delivery</summary>
    public string Kind { get; init; } = "all";

    public string? Q { get; init; }
    public string? JobStatus { get; init; }
    public ulong? CustomerId { get; init; }
    public ulong? DriverId { get; init; }
    public ulong? ReaderWayId { get; init; }
    public ulong? FromLocationId { get; init; }
    public ulong? ToLocationId { get; init; }
    public bool OnlyOpen { get; init; }
    public bool OnlyOverdue { get; init; }
    public bool OnlyStalled { get; init; }
    public bool OnlyDeltaMismatch { get; init; }
    public int StalledHours { get; init; } = 24;
    public DateTime? CreatedFrom { get; init; }
    public DateTime? CreatedTo { get; init; }
    public string SortBy { get; init; } = "created";
    public string SortDir { get; init; } = "desc";
}
