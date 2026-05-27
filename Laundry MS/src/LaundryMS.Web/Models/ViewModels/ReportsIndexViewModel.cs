namespace LaundryMS.Web.Models.ViewModels;

public class ReportsIndexViewModel
{
    public int TotalLinenItems { get; init; }
    public int DamagedItemCount { get; init; }

    public int TotalScansInWindow { get; init; }
    public int RejectedScansInWindow { get; init; }
    public int AvgIngestLagSeconds { get; init; }
    public int P95IngestLagSeconds { get; init; }

    public int OpenJobsCount { get; init; }
    public int OverdueJobsCount { get; init; }
    public int StalledJobsCount { get; init; }

    public IReadOnlyList<CustomerCleanedStatRowViewModel> ItemsCleanedByCustomer { get; init; } = [];
    public IReadOnlyList<StatusCountRowViewModel> ItemsByStatus { get; init; } = [];
    public IReadOnlyList<LocationCountRowViewModel> ItemsByLocation { get; init; } = [];
    public IReadOnlyList<ThroughputRowViewModel> ThroughputByPurpose { get; init; } = [];

    public IReadOnlyList<TimeSeriesCountRowViewModel> ThroughputByDay { get; init; } = [];
    public IReadOnlyList<TimeSeriesCountRowViewModel> RejectedByDay { get; init; } = [];
    public IReadOnlyList<ReportsReasonCountRowViewModel> TopRejectionReasons { get; init; } = [];
    public IReadOnlyList<ReportsReasonCountRowViewModel> DamagedByCustomer { get; init; } = [];
    public IReadOnlyList<ReportsReasonCountRowViewModel> DamagedByLocation { get; init; } = [];
    public IReadOnlyList<JobStatusCountRowViewModel> JobsByStatus { get; init; } = [];

    public IReadOnlyList<CustomerOptionViewModel> Customers { get; init; } = [];
    public IReadOnlyList<ReaderOptionViewModel> Readers { get; init; } = [];
    public IReadOnlyList<ReaderWayOptionViewModel> ReaderWays { get; init; } = [];

    public ReportsQueryViewModel Query { get; init; } = new();
    public string? WindowNotice { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
    public string WashCycleNote { get; init; } =
        "Wash cycle counts will be derived from conveyor and process events when those integrations are enabled.";
}

public class CustomerCleanedStatRowViewModel
{
    public string CustomerName { get; init; } = string.Empty;
    public int CleanedOrReadyCount { get; init; }
}

public class ThroughputRowViewModel
{
    public string PurposeKey { get; init; } = string.Empty;
    public int EventCount { get; init; }
}

public class TimeSeriesCountRowViewModel
{
    public DateTime Date { get; init; }
    public int Count { get; init; }
}

public class ReportsReasonCountRowViewModel
{
    public string Key { get; init; } = string.Empty;
    public int Count { get; init; }
}

public class JobStatusCountRowViewModel
{
    public string JobStatus { get; init; } = string.Empty;
    public int Count { get; init; }
}
