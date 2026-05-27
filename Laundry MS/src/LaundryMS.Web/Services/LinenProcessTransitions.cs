namespace LaundryMS.Web.Services;

/// <summary>Valid process status transitions (aligned with LinenAssignment admin UI).</summary>
public static class LinenProcessTransitions
{
    private static readonly Dictionary<string, HashSet<string>> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["at_customer"] = ["arriving_at_laundry", "in_transit"],
        ["arriving_at_laundry"] = ["arrived_at_laundry"],
        ["arrived_at_laundry"] = ["waiting_for_cleaning", "being_cleaned"],
        ["waiting_for_cleaning"] = ["being_cleaned"],
        ["being_cleaned"] = ["cleaned"],
        ["cleaned"] = ["ready_for_dispatch", "at_customer"],
        ["ready_for_dispatch"] = ["in_transit", "at_customer"],
        ["in_transit"] = ["at_customer", "arrived_at_laundry"]
    };

    public static string? ValidateTransition(string? previousStatus, string nextStatus)
    {
        var next = Normalize(nextStatus);
        if (next == null)
            return "Target process status is required.";

        var prev = Normalize(previousStatus);
        if (prev == null)
            return null;
        if (prev == next)
            return null;

        return Allowed.TryGetValue(prev, out var allowed) && allowed.Contains(next)
            ? null
            : $"Invalid status transition: {prev} -> {next}.";
    }

    public static string? Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;
        return status.Trim().ToLowerInvariant();
    }
}
