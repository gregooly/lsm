namespace LaundryMS.Web.Models.ViewModels;

public class DriversQueryViewModel
{
    public string? Q { get; init; }
    public bool IncludeInactive { get; init; }
    public string SortBy { get; init; } = "name";
    public string SortDir { get; init; } = "asc";
}
