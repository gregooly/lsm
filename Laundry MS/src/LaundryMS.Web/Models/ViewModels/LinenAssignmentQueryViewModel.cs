namespace LaundryMS.Web.Models.ViewModels;

public class LinenAssignmentQueryViewModel
{
    public string? Q { get; init; }
    public ulong? CustomerId { get; init; }
    public ulong? LocationId { get; init; }
    public string? AssignmentType { get; init; }
    public string? ProcessStatus { get; init; }
    public string? PhysicalCondition { get; init; }
    public bool IncludeInactive { get; init; }
    public bool ExceptionOnly { get; init; }
    public string SortBy { get; init; } = "rfid";
    public string SortDir { get; init; } = "asc";
}
