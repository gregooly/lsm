namespace LaundryMS.Web.Models.ViewModels;

public class LocationsQueryViewModel
{
    public string? Q { get; init; }
    public string? LocationType { get; init; }
    public ulong? CustomerId { get; init; }
    public bool IncludeInactive { get; init; }
    public string SortBy { get; init; } = "name";
    public string SortDir { get; init; } = "asc";
}
