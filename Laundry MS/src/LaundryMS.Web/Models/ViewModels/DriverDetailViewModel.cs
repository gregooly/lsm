namespace LaundryMS.Web.Models.ViewModels;

public class DriverDetailViewModel
{
    public ulong Id { get; init; }
    public string DriverName { get; init; } = string.Empty;
    public string? MobilePhone { get; init; }
    public string? VehicleRegistrationNo { get; init; }
    public string? HandheldDeviceId { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public int RecentCollectionJobCount { get; init; }
    public int RecentDeliveryJobCount { get; init; }
    public int OpenLogisticsJobsCount { get; init; }
    public int LinkedScanEventCount { get; init; }
}
