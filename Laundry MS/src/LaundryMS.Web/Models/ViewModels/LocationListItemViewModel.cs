namespace LaundryMS.Web.Models.ViewModels;

public class LocationListItemViewModel
{
    public ulong Id { get; init; }
    public string LocationName { get; init; } = string.Empty;
    public string LocationType { get; init; } = string.Empty;
    public string? CustomerName { get; init; }
    public string? LocationAddressText { get; init; }
    public int LinkedLinenCount { get; init; }
    public int LinkedReaderWaysCount { get; init; }
    public int OpenLogisticsJobsCount { get; init; }
    public bool IsActive { get; init; }
}
