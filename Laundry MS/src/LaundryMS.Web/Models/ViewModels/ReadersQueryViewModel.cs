namespace LaundryMS.Web.Models.ViewModels;

public class ReadersQueryViewModel
{
    public string? Q { get; init; }
    public string? Category { get; init; }
    public bool IncludeInactive { get; init; }
    public bool OnlyUnmapped { get; init; }
    public bool OnlyIdle { get; init; }
    public string SortBy { get; init; } = "name";
    public string SortDir { get; init; } = "asc";
}
