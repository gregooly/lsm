namespace LaundryMS.Web.Models.ViewModels;

public class CollectionsIndexViewModel
{
    public IReadOnlyList<LogisticsJobListItemViewModel> Jobs { get; init; } = [];
    public IReadOnlyList<CustomerOptionViewModel> Customers { get; init; } = [];
    public IReadOnlyList<DriverOptionViewModel> Drivers { get; init; } = [];
    public IReadOnlyList<LocationOptionViewModel> Locations { get; init; } = [];
    public IReadOnlyList<ReaderWayOptionViewModel> ReaderWays { get; init; } = [];
}

