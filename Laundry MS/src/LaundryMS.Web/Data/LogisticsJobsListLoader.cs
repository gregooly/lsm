using LaundryMS.Web.Models.Entities;
using LaundryMS.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Data;

/// <summary>
/// Loads logistics job list rows without filtered collection Count() inside a single projection
/// (avoids Pomelo/MySQL translation issues).
/// </summary>
public static class LogisticsJobsListLoader
{
    public static async Task<IReadOnlyList<LogisticsJobListItemViewModel>> LoadAsync(
        LaundryMsDbContext db,
        CancellationToken cancellationToken,
        Func<IQueryable<LogisticsJob>, IQueryable<LogisticsJob>>? filter = null)
    {
        IQueryable<LogisticsJob> query = db.LogisticsJobs.AsNoTracking();
        if (filter is not null)
            query = filter(query);

        var jobs = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .Select(ToJobHeadRow)
            .ToListAsync(cancellationToken);

        return await HydrateAsync(db, jobs, cancellationToken);
    }

    public static async Task<(IReadOnlyList<LogisticsJobListItemViewModel> Items, int TotalCount)> LoadPagedAsync(
        LaundryMsDbContext db,
        LogisticsJobsQueryViewModel query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 10 or > 150 ? 50 : pageSize;

        var stalledHours = query.StalledHours is < 1 or > 240 ? 24 : query.StalledHours;
        var now = DateTime.UtcNow;
        var stalledCutoff = now.AddHours(-stalledHours);

        IQueryable<LogisticsJob> q = db.LogisticsJobs.AsNoTracking();
        q = ApplyQueryFilters(db, q, query, stalledCutoff);
        q = ApplySort(db, q, query);

        var total = await q.CountAsync(cancellationToken);

        var jobs = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToJobHeadRow)
            .ToListAsync(cancellationToken);

        var items = await HydrateAsync(db, jobs, cancellationToken);

        if (query.OnlyDeltaMismatch)
            items = items.Where(x => x.Delta.HasValue && x.Delta.Value != 0).ToList();

        return (items, total);
    }

    public static IQueryable<LogisticsJob> ApplyQueryFilters(
        LaundryMsDbContext db,
        IQueryable<LogisticsJob> query,
        LogisticsJobsQueryViewModel q,
        DateTime stalledCutoff)
    {
        var kind = (q.Kind ?? "all").Trim().ToLowerInvariant();
        if (kind == "collection")
            query = query.WhereCollectionJobType();
        else if (kind == "delivery")
            query = query.WhereDeliveryJobType();

        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var term = q.Q.Trim();
            query = query.Where(x =>
                x.Notes != null && x.Notes.Contains(term)
                || (x.CustomerId != null && db.Customers.Any(c => c.CustomerId == x.CustomerId && c.CustomerName.Contains(term)))
                || x.Driver != null && x.Driver.DriverName.Contains(term));

            if (ulong.TryParse(term, out var idTerm))
                query = query.Where(x => x.Id == idTerm || (x.ReaderWayId != null && x.ReaderWayId == idTerm));
        }

        if (!string.IsNullOrWhiteSpace(q.JobStatus))
        {
            var st = q.JobStatus.Trim().ToLowerInvariant();
            query = query.Where(x => (x.JobStatus ?? "").ToLower() == st);
        }

        if (q.CustomerId.HasValue)
            query = query.Where(x => x.CustomerId == q.CustomerId.Value);

        if (q.DriverId.HasValue)
            query = query.Where(x => x.DriverId == q.DriverId.Value);

        if (q.ReaderWayId.HasValue)
            query = query.Where(x => x.ReaderWayId == q.ReaderWayId.Value);

        if (q.FromLocationId.HasValue)
            query = query.Where(x => x.FromLocationId == q.FromLocationId.Value);

        if (q.ToLocationId.HasValue)
            query = query.Where(x => x.ToLocationId == q.ToLocationId.Value);

        if (q.OnlyOpen)
            query = query.Where(x => x.JobStatus == "open" || x.JobStatus == "in_progress");

        if (q.OnlyOverdue)
            query = query.Where(x =>
                (x.JobStatus == "open" || x.JobStatus == "in_progress")
                && x.PlannedEndAt.HasValue
                && x.PlannedEndAt.Value < DateTime.UtcNow);

        if (q.OnlyStalled)
            query = query.Where(x =>
                (x.JobStatus == "open" || x.JobStatus == "in_progress")
                && (!db.LinenMovementEvents.Any(e => e.LogisticsJobId == x.Id)
                    || db.LinenMovementEvents.Where(e => e.LogisticsJobId == x.Id).Max(e => (DateTime?)e.OccurredAt) < stalledCutoff));

        if (q.CreatedFrom.HasValue)
        {
            var from = q.CreatedFrom.Value.Date;
            query = query.Where(x => x.CreatedAt >= from);
        }

        if (q.CreatedTo.HasValue)
        {
            var toExclusive = q.CreatedTo.Value.Date.AddDays(1);
            query = query.Where(x => x.CreatedAt < toExclusive);
        }

        return query;
    }

    private static IQueryable<LogisticsJob> ApplySort(
        LaundryMsDbContext db,
        IQueryable<LogisticsJob> query,
        LogisticsJobsQueryViewModel q)
    {
        var sortBy = string.IsNullOrWhiteSpace(q.SortBy) ? "created" : q.SortBy.Trim().ToLowerInvariant();
        var desc = string.Equals(q.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "status" => desc
                ? query.OrderByDescending(x => x.JobStatus).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.JobStatus).ThenByDescending(x => x.CreatedAt),
            "type" => desc
                ? query.OrderByDescending(x => x.JobType).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.JobType).ThenByDescending(x => x.CreatedAt),
            "customer" => desc
                ? query.OrderByDescending(x =>
                        db.Customers.Where(c => c.CustomerId == x.CustomerId).OrderBy(c => c.CustomerName).Select(c => c.CustomerName).FirstOrDefault()
                        ?? string.Empty).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x =>
                        db.Customers.Where(c => c.CustomerId == x.CustomerId).OrderBy(c => c.CustomerName).Select(c => c.CustomerName).FirstOrDefault()
                        ?? string.Empty).ThenByDescending(x => x.CreatedAt),
            "driver" => desc
                ? query.OrderByDescending(x => x.Driver != null ? x.Driver.DriverName : string.Empty).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.Driver != null ? x.Driver.DriverName : string.Empty).ThenByDescending(x => x.CreatedAt),
            "lastscan" => desc
                ? query.OrderByDescending(x => db.LinenMovementEvents.Where(e => e.LogisticsJobId == x.Id).Max(e => (DateTime?)e.OccurredAt)).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => db.LinenMovementEvents.Where(e => e.LogisticsJobId == x.Id).Max(e => (DateTime?)e.OccurredAt)).ThenByDescending(x => x.CreatedAt),
            "updated" => desc
                ? query.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.UpdatedAt).ThenByDescending(x => x.CreatedAt),
            _ => desc
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CreatedAt)
        };
    }

    private static readonly System.Linq.Expressions.Expression<Func<LogisticsJob, JobHeadRow>> ToJobHeadRow = x => new JobHeadRow
    {
        Id = x.Id,
        JobType = x.JobType,
        JobStatus = x.JobStatus,
        TenantCustomerId = x.CustomerId,
        CustomerName = null,
        DriverName = x.Driver != null ? x.Driver.DriverName : null,
        ReaderWayId = x.ReaderWayId,
        ReaderWayName = x.ReaderWay != null ? x.ReaderWay.WayName : null,
        FromLocationName = x.FromLocation != null ? x.FromLocation.LocationName : null,
        ToLocationName = x.ToLocation != null ? x.ToLocation.LocationName : null,
        PlannedStartAt = x.PlannedStartAt,
        PlannedEndAt = x.PlannedEndAt,
        ActualStartAt = x.ActualStartAt,
        ActualEndAt = x.ActualEndAt,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };

    private static async Task<IReadOnlyList<LogisticsJobListItemViewModel>> HydrateAsync(
        LaundryMsDbContext db,
        List<JobHeadRow> jobs,
        CancellationToken cancellationToken)
    {
        if (jobs.Count == 0)
            return [];

        var tenantIds = jobs
            .Select(j => j.TenantCustomerId)
            .Where(t => t.HasValue && t.Value != 0)
            .Select(t => t!.Value)
            .Distinct()
            .ToList();

        var displayNameByTenant = tenantIds.Count == 0
            ? new Dictionary<ulong, string?>()
            : await db.Customers.AsNoTracking()
                .Where(c => c.CustomerId != null && tenantIds.Contains(c.CustomerId.Value))
                .GroupBy(c => c.CustomerId!.Value)
                .Select(g => new { Tid = g.Key, Name = g.OrderBy(c => c.CustomerName).Select(c => c.CustomerName).FirstOrDefault() })
                .ToDictionaryAsync(x => x.Tid, x => x.Name, cancellationToken);

        var ids = jobs.Select(j => j.Id).ToList();

        var expectedTotals = await db.JobExpectedItems.AsNoTracking()
            .Where(e => ids.Contains(e.LogisticsJobId))
            .GroupBy(e => e.LogisticsJobId)
            .Select(g => new { JobId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.JobId, x => x.Total, cancellationToken);

        var reachedTotals = await db.JobExpectedItems.AsNoTracking()
            .Where(e => ids.Contains(e.LogisticsJobId) && e.ReachedExpectedStatus)
            .GroupBy(e => e.LogisticsJobId)
            .Select(g => new { JobId = g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.JobId, x => x.N, cancellationToken);

        var scanRows = await db.LinenMovementEvents.AsNoTracking()
            .Where(e => e.LogisticsJobId != null && ids.Contains(e.LogisticsJobId.Value))
            .Select(e => new { JobId = e.LogisticsJobId!.Value, e.LinenItemId, e.OccurredAt })
            .ToListAsync(cancellationToken);

        var lastScanByJob = new Dictionary<ulong, DateTime?>();
        var uniqueTagsByJob = new Dictionary<ulong, HashSet<ulong>>();
        foreach (var row in scanRows)
        {
            if (!lastScanByJob.TryGetValue(row.JobId, out var last) || !last.HasValue || row.OccurredAt > last.Value)
                lastScanByJob[row.JobId] = row.OccurredAt;

            if (!uniqueTagsByJob.TryGetValue(row.JobId, out var set))
            {
                set = new HashSet<ulong>();
                uniqueTagsByJob[row.JobId] = set;
            }

            set.Add(row.LinenItemId);
        }

        return jobs.Select(j =>
        {
            var displayCustomer = j.TenantCustomerId.HasValue
                ? displayNameByTenant.GetValueOrDefault(j.TenantCustomerId.Value)
                : null;
            var expected = expectedTotals.GetValueOrDefault(j.Id);
            var reached = reachedTotals.GetValueOrDefault(j.Id);
            return new LogisticsJobListItemViewModel
            {
                Id = j.Id,
                JobType = j.JobType,
                JobStatus = j.JobStatus,
                CustomerName = displayCustomer ?? j.CustomerName,
                DriverName = j.DriverName,
                ReaderWayId = j.ReaderWayId,
                ReaderWayName = j.ReaderWayName,
                FromLocationName = j.FromLocationName,
                ToLocationName = j.ToLocationName,
                LastScanAt = lastScanByJob.GetValueOrDefault(j.Id),
                ScannedUniqueTagCount = uniqueTagsByJob.TryGetValue(j.Id, out var tagSet) ? tagSet.Count : 0,
                PlannedStartAt = j.PlannedStartAt,
                PlannedEndAt = j.PlannedEndAt,
                ActualStartAt = j.ActualStartAt,
                ActualEndAt = j.ActualEndAt,
                CreatedAt = j.CreatedAt,
                UpdatedAt = j.UpdatedAt,
                ExpectedItemCount = expected,
                ReachedItemCount = reached,
                Delta = expected > 0 ? reached - expected : null
            };
        }).ToList();
    }

    private sealed class JobHeadRow
    {
        public ulong Id { get; init; }
        public string JobType { get; init; } = string.Empty;
        public string JobStatus { get; init; } = string.Empty;
        public ulong? TenantCustomerId { get; init; }
        public string? CustomerName { get; init; }
        public string? DriverName { get; init; }
        public ulong? ReaderWayId { get; init; }
        public string? ReaderWayName { get; init; }
        public string? FromLocationName { get; init; }
        public string? ToLocationName { get; init; }
        public DateTime? PlannedStartAt { get; init; }
        public DateTime? PlannedEndAt { get; init; }
        public DateTime? ActualStartAt { get; init; }
        public DateTime? ActualEndAt { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
