namespace LaundryMS.Web.Models.ViewModels;

public class LinenItemsQueryViewModel
{
    public string? Q { get; init; }
    public ulong? CustomerId { get; init; }
    public ulong? LocationId { get; init; }
    public bool LocationUnassigned { get; init; }
    public string? ProcessStatus { get; init; }
    public string? PhysicalCondition { get; init; }
    public string? ProcessingResult { get; init; }
    public int? StaleDays { get; init; }
    public bool ExceptionOnly { get; init; }
    public string SortBy { get; init; } = "rfid";
    public string SortDir { get; init; } = "asc";
    /// <summary>When true, inactive items are included in results.</summary>
    public bool IncludeInactive { get; init; }
}
