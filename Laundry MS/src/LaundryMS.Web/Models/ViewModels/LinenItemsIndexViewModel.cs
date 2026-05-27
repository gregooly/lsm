namespace LaundryMS.Web.Models.ViewModels;

public class LinenItemsIndexViewModel
{
    public IReadOnlyList<LinenItemHubRowViewModel> Items { get; init; } = [];
    public LinenItemsQueryViewModel Query { get; init; } = new();
    public IReadOnlyList<CustomerOptionViewModel> Customers { get; init; } = [];
    public IReadOnlyList<LocationOptionViewModel> Locations { get; init; } = [];
    public IReadOnlyList<string> ProcessStatuses { get; init; } = [];
    public IReadOnlyList<string> PhysicalConditions { get; init; } = [];
    public IReadOnlyList<string> ProcessingResults { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public int TotalCount { get; init; }

    public int TotalPages =>
        TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
