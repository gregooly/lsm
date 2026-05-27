namespace LaundryMS.Web.Models.ViewModels;

public class ScanHistoryIndexViewModel
{
    public IReadOnlyList<MovementEventListItemViewModel> Events { get; init; } = [];
    public IReadOnlyList<ReaderOptionViewModel> Readers { get; init; } = [];
    public IReadOnlyList<ReaderWayOptionViewModel> ReaderWays { get; init; } = [];
    public IReadOnlyList<string> ProcessingResultOptions { get; init; } = [];

    public ScanHistoryQueryViewModel Query { get; init; } = new();

    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public int Total24h { get; init; }
    public int Rejected24h { get; init; }
    public int Duplicates24h { get; init; }
    public int AvgLagSeconds24h { get; init; }
    public int P95LagSeconds24h { get; init; }
    public IReadOnlyList<ReasonCountRowViewModel> TopRejectionReasons24h { get; init; } = [];
}

public class ReasonCountRowViewModel
{
    public string Reason { get; init; } = string.Empty;
    public int Count { get; init; }
}
