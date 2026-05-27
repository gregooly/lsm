namespace LaundryMS.Web.Models.ViewModels;

public class LocationsIndexViewModel
{
    public IReadOnlyList<LocationListItemViewModel> Items { get; init; } = [];
    public IReadOnlyList<CustomerOptionViewModel> CustomerOptions { get; init; } = [];
    public IReadOnlyList<string> LocationTypeOptions { get; init; } = [];
    public LocationsQueryViewModel Query { get; init; } = new();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public int TotalCount { get; init; }

    public int TotalPages =>
        TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
