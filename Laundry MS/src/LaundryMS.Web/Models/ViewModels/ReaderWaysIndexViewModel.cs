namespace LaundryMS.Web.Models.ViewModels;

public class ReaderWaysIndexViewModel
{
    public IReadOnlyList<ReaderWayListItemViewModel> Items { get; init; } = [];
    public IReadOnlyList<ReaderOptionViewModel> Readers { get; init; } = [];
    public IReadOnlyList<LocationOptionViewModel> Locations { get; init; } = [];
    public IReadOnlyList<string> PurposeOptions { get; init; } = [];
    public IReadOnlyList<string> TargetStatusOptions { get; init; } = [];
    public ReaderWaysQueryViewModel Query { get; init; } = new();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
    public int SilentCount { get; init; }

    public int TotalPages =>
        TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
