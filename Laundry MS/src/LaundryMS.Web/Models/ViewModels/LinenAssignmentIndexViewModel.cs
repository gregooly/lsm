namespace LaundryMS.Web.Models.ViewModels;

public class LinenAssignmentIndexViewModel
{
    public IReadOnlyList<LinenItemListItemViewModel> Items { get; init; } = [];
    public IReadOnlyList<CustomerOptionViewModel> Customers { get; init; } = [];
    public IReadOnlyList<LocationOptionViewModel> Locations { get; init; } = [];
    public IReadOnlyList<EmployeeOptionViewModel> Employees { get; init; } = [];
    public IReadOnlyList<string> ProcessStatuses { get; init; } = [];
    public IReadOnlyList<string> PhysicalConditions { get; init; } = [];
    public LinenAssignmentQueryViewModel Query { get; init; } = new();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public int TotalCount { get; init; }

    public int TotalPages =>
        TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

