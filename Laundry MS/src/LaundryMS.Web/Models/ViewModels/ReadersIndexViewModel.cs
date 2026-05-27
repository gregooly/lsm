namespace LaundryMS.Web.Models.ViewModels;

public class ReadersIndexViewModel
{
    public IReadOnlyList<ReaderListItemViewModel> Items { get; init; } = [];
    public IReadOnlyList<string> CategoryOptions { get; init; } = [];
    public ReadersQueryViewModel Query { get; init; } = new();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public int TotalCount { get; init; }

    public int TotalPages =>
        TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
