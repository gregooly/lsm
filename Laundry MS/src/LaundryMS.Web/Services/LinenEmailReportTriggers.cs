namespace LaundryMS.Web.Services;

/// <summary>Aligns outbound customer emails with reader route semantics (pickup vs main-gate arrival).</summary>
public static class LinenEmailReportTriggers
{
    public static bool ShouldSendPickupEmail(string routeTargetStatus, string? businessPurposeKey, string readerCategory)
    {
        var status = (routeTargetStatus ?? string.Empty).Trim().ToLowerInvariant();
        var purpose = (businessPurposeKey ?? string.Empty).Trim().ToLowerInvariant();
        var cat = (readerCategory ?? string.Empty).Trim().ToLowerInvariant();

        if (purpose == "pickup")
            return true;

        if (status == "picked_up")
            return true;

        // Typical driver collection route: handheld reader sets linen to in_transit leaving the customer site.
        if (cat == "handheld" && status == "in_transit")
            return true;

        return false;
    }

    public static bool ShouldSendArrivalEmail(string routeTargetStatus, string readerCategory)
    {
        var status = (routeTargetStatus ?? string.Empty).Trim().ToLowerInvariant();
        var cat = (readerCategory ?? string.Empty).Trim().ToLowerInvariant();

        if (cat == "handheld")
            return false;

        return status is "arrived_at_laundry" or "arriving_at_laundry";
    }
}
