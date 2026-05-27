namespace LaundryMS.Web.Services;

public interface ILinenEmailReportService
{
    /// <summary>Sends pickup confirmation after a driver handheld batch when settings and route match.</summary>
    Task TrySendPickupReportAsync(
        ulong tenantCustomerId,
        string pickupLocationDisplayName,
        IReadOnlyDictionary<string, int> itemCountsByType,
        DateTime batchOccurredAtUtc,
        CancellationToken cancellationToken);

    /// <summary>Sends arrival confirmation after a fixed / gate reader batch when settings and route match.</summary>
    Task TrySendArrivalReportAsync(
        ulong tenantCustomerId,
        IReadOnlyDictionary<string, int> itemCountsByType,
        DateTime batchOccurredAtUtc,
        CancellationToken cancellationToken);
}
