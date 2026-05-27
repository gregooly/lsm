namespace LaundryMS.Web.Models;

/// <summary>
/// In-memory job type checks only. For Entity Framework queries use <see cref="LaundryMS.Web.Data.LogisticsJobQueryExtensions"/>.
/// </summary>
public static class LogisticsJobTypeHelper
{
    private static readonly HashSet<string> CollectionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "collection", "pickup", "collect"
    };

    private static readonly HashSet<string> DeliveryTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "delivery", "dispatch"
    };

    public static bool IsCollection(string? jobType) =>
        !string.IsNullOrWhiteSpace(jobType) && CollectionTypes.Contains(jobType.Trim());

    public static bool IsDelivery(string? jobType) =>
        !string.IsNullOrWhiteSpace(jobType) && DeliveryTypes.Contains(jobType.Trim());
}
