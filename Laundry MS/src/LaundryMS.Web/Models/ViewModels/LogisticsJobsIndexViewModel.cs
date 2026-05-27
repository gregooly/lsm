namespace LaundryMS.Web.Models.ViewModels;

public class LogisticsJobsIndexViewModel
{
    public IReadOnlyList<LogisticsJobListItemViewModel> Jobs { get; init; } = [];
    public LogisticsJobsQueryViewModel Query { get; init; } = new();
    public IReadOnlyList<CustomerOptionViewModel> Customers { get; init; } = [];
    public IReadOnlyList<DriverOptionViewModel> Drivers { get; init; } = [];
    public IReadOnlyList<LocationOptionViewModel> Locations { get; init; } = [];
    public IReadOnlyList<ReaderWayOptionViewModel> ReaderWays { get; init; } = [];
    public IReadOnlyList<string> JobStatuses { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public int TotalCount { get; init; }

    public int OpenCount { get; init; }
    public int InProgressCount { get; init; }
    public int CompletedTodayCount { get; init; }
    public int OverdueCount { get; init; }
    public int StalledCount { get; init; }

    public int TotalPages =>
        TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
