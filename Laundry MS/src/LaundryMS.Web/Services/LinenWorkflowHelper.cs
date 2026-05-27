using LaundryMS.Web.Models.Entities;

namespace LaundryMS.Web.Services;

/// <summary>
/// Domain rules for temporary linen assignment (e.g. clear employee checkout when process reaches configured statuses).
/// </summary>
public static class LinenWorkflowHelper
{
    public static IReadOnlyList<string> ParseStatusList(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return DefaultClearStatuses;

        return csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// When assignment is temporary and process status enters one of the configured values, clear the employee assignee.
    /// </summary>
    public static void ApplyTemporaryEmployeeClear(
        LinenItem entity,
        string? previousProcessStatus,
        IReadOnlyList<string> clearWhenReachingStatuses)
    {
        if (entity.DefaultAssignmentType != "temporary")
            return;

        if (entity.AssignedEmployeeId == null)
            return;

        var next = (entity.CurrentProcessStatus ?? "").Trim().ToLowerInvariant();
        var prev = (previousProcessStatus ?? "").Trim().ToLowerInvariant();
        if (next == prev)
            return;

        if (clearWhenReachingStatuses.Contains(next))
            entity.AssignedEmployeeId = null;
    }

    private static readonly string[] DefaultClearStatuses = ["cleaned", "ready_for_dispatch", "at_customer"];
}
