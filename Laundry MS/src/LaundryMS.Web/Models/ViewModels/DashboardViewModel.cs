namespace LaundryMS.Web.Models.ViewModels;

public class DashboardViewModel
{
    public bool DatabaseReachable { get; init; }
    public string? ErrorMessage { get; init; }
    public int CustomerCount { get; init; }
    public int LocationCount { get; init; }
    public int ReaderCount { get; init; }
    public int LinenItemCount { get; init; }
    public int OpenJobCount { get; init; }
    public int PendingExpectedItems { get; init; }
    public int DamagedLinenCount { get; init; }
    public int ScansLast24Hours { get; init; }
    public IReadOnlyList<RecentMovementItemViewModel> RecentMovements { get; init; } = [];
    public IReadOnlyList<StatusCountRowViewModel> ItemsByStatus { get; init; } = [];
    public IReadOnlyList<LocationCountRowViewModel> ItemsByLocation { get; init; } = [];
    public IReadOnlyList<CustomerOverviewRowViewModel> CustomerOverview { get; init; } = [];
}

public class RecentMovementItemViewModel
{
    public string RfidTag { get; init; } = string.Empty;
    public string ReaderName { get; init; } = string.Empty;
    public string WayName { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public string ProcessingResult { get; init; } = string.Empty;
}

public class StatusCountRowViewModel
{
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
}

public class LocationCountRowViewModel
{
    public string LocationName { get; init; } = string.Empty;
    public int Count { get; init; }
}

public class CustomerOverviewRowViewModel
{
    public string CustomerName { get; init; } = string.Empty;
    public int LocationCount { get; init; }
    public int LinenCount { get; init; }
    public int ActiveLinenCount { get; init; }
    public string ContractHint { get; init; } = string.Empty;
}
