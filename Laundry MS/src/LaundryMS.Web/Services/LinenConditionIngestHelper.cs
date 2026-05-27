using LaundryMS.Web.Data;
using LaundryMS.Web.Models.Entities;

namespace LaundryMS.Web.Services;

/// <summary>Applies good/damaged/lost from scan ingest and records quality history for admin review.</summary>
public static class LinenConditionIngestHelper
{
    public static bool TryNormalizeCondition(string? raw, out string normalized)
    {
        normalized = (raw ?? string.Empty).Trim().ToLowerInvariant();
        return normalized is "good" or "damaged" or "lost";
    }

    public static void ApplyConditionChange(
        LaundryMsDbContext db,
        LinenItem linenItem,
        ulong tenantCustomerId,
        string newCondition,
        string reportedBy,
        DateTime now)
    {
        var fromCondition = linenItem.PhysicalCondition;
        if (string.Equals(fromCondition, newCondition, StringComparison.OrdinalIgnoreCase))
            return;

        linenItem.PhysicalCondition = newCondition;
        if (newCondition == "good" && linenItem.LifecycleState == "discarded")
            linenItem.LifecycleState = "active";

        var eventType = newCondition switch
        {
            "good" => fromCondition is "damaged" or "lost" ? "repaired" : "condition_ok",
            "damaged" => "reported",
            "lost" => "lost_confirmed",
            _ => "condition_change"
        };

        db.LinenQualityEvents.Add(new LinenQualityEvent
        {
            CustomerId = tenantCustomerId,
            LinenItemId = linenItem.Id,
            EventType = eventType,
            FromCondition = fromCondition,
            ToCondition = newCondition,
            Note = null,
            ReportedBy = reportedBy,
            ResolvedBy = newCondition == "good" ? reportedBy : null,
            CreatedAt = now
        });
    }
}
