namespace LaundryMS.Web.Models.ViewModels;

public class LocationDetailViewModel
{
    public ulong Id { get; init; }
    public string LocationName { get; init; } = string.Empty;
    public string LocationType { get; init; } = string.Empty;
    public ulong? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? LocationAddressText { get; init; }
    public string? ContactPerson { get; init; }
    public string? ContactPhone { get; init; }
    public decimal? GeoLat { get; init; }
    public decimal? GeoLng { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public int LinkedLinenCount { get; init; }
    public int LinkedReaderWaysCount { get; init; }
    public int OpenLogisticsJobsCount { get; init; }
}
