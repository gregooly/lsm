namespace LaundryMS.Web.Models.ViewModels;

public class ReaderListItemViewModel
{
    public ulong Id { get; init; }
    public string ReaderName { get; init; } = string.Empty;
    public string DeviceIdentifier { get; init; } = string.Empty;
    public string? DeviceModel { get; init; }
    public string ReaderCategory { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int WayCount { get; init; }
    public DateTime? LastSeenAt { get; init; }
    public int Scans24h { get; init; }
    public int Scans7d { get; init; }
    public string OnlineState { get; init; } = "never_seen";
    public string CoverageSummary { get; init; } = "0 routes";
}
