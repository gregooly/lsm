using System.Text;
using LaundryMS.Web.Data;
using LaundryMS.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Controllers;

public class ReportsController : TenantScopedController
{
    private const int MaxWindowDays = 90;
    private readonly LaundryMsDbContext _dbContext;

    public ReportsController(LaundryMsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(
        ulong? customerId,
        ulong? readerId,
        ulong? readerWayId,
        bool onlyExceptions = false,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentCustomerId(out var tenantCustomerId))
            return Forbid();
        customerId = tenantCustomerId;

        var now = DateTime.UtcNow;
        var (fromUtc, toUtc, windowNotice) = NormalizeUtcWindow(from, to, now);

        var customers = await _dbContext.Customers
            .AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == tenantCustomerId)
            .OrderBy(x => x.CustomerName)
            .Select(x => new CustomerOptionViewModel { Id = x.Id, CustomerName = x.CustomerName })
            .ToListAsync(cancellationToken);

        var readers = await _dbContext.Readers
            .AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == tenantCustomerId)
            .OrderBy(x => x.ReaderName)
            .Select(x => new ReaderOptionViewModel { Id = x.Id, ReaderName = x.ReaderName })
            .ToListAsync(cancellationToken);

        var readerWays = await _dbContext.ReaderWays
            .AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == tenantCustomerId)
            .OrderBy(x => x.WayName)
            .Select(x => new ReaderWayOptionViewModel
            {
                Id = x.Id,
                DisplayName = x.WayName + " — " + x.Reader.ReaderName
            })
            .ToListAsync(cancellationToken);

        IQueryable<Models.Entities.LinenItem> itemsQ = _dbContext.LinenItems.AsNoTracking().Where(x => x.CustomerId == tenantCustomerId);
        if (customerId.HasValue)
            itemsQ = itemsQ.Where(x => x.OwnerCustomerId == customerId.Value);
        if (onlyExceptions)
            itemsQ = itemsQ.Where(x => (x.PhysicalCondition ?? "").ToLower() != "good");

        var total = await itemsQ.CountAsync(cancellationToken);
        var damaged = await itemsQ.CountAsync(x => (x.PhysicalCondition ?? "").ToLower() != "good", cancellationToken);

        var itemsByStatus = await itemsQ
            .GroupBy(x => x.CurrentProcessStatus)
            .Select(g => new StatusCountRowViewModel { Status = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(20)
            .ToListAsync(cancellationToken);

        var locationNameMap = await _dbContext.Locations.AsNoTracking()
            .Where(x => x.CustomerId == tenantCustomerId)
            .ToDictionaryAsync(x => x.Id, x => x.LocationName, cancellationToken);

        var itemsByLocationRaw = await itemsQ
            .GroupBy(x => x.CurrentLocationId)
            .Select(g => new { LocationId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var itemsByLocation = itemsByLocationRaw
            .Select(x => new LocationCountRowViewModel
            {
                LocationName = x.LocationId.HasValue && locationNameMap.TryGetValue(x.LocationId.Value, out var name)
                    ? name
                    : x.LocationId.HasValue ? "Unknown location id" : "(Not set)",
                Count = x.Count
            })
            .OrderByDescending(x => x.Count)
            .Take(20)
            .ToList();

        var cleanedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "cleaned", "ready_for_dispatch", "ready", "clean"
        };

        var cleanRows = await itemsQ
            .Where(x => cleanedStatuses.Contains(x.CurrentProcessStatus))
            .GroupBy(x => x.OwnerCustomer != null ? x.OwnerCustomer.CustomerName : "(No owner / pool)")
            .Select(g => new CustomerCleanedStatRowViewModel
            {
                CustomerName = g.Key,
                CleanedOrReadyCount = g.Count()
            })
            .OrderByDescending(x => x.CleanedOrReadyCount)
            .Take(30)
            .ToListAsync(cancellationToken);

        var throughputQ = _dbContext.LinenMovementEvents.AsNoTracking()
            .Where(x => x.CustomerId == tenantCustomerId)
            .Where(x => x.OccurredAt >= fromUtc && x.OccurredAt <= toUtc);

        if (customerId.HasValue)
            throughputQ = throughputQ.Where(x => x.LinenItem.OwnerCustomerId == customerId.Value);
        if (readerId.HasValue)
            throughputQ = throughputQ.Where(x => x.ReaderId == readerId.Value);
        if (readerWayId.HasValue)
            throughputQ = throughputQ.Where(x => x.ReaderWayId == readerWayId.Value);
        if (onlyExceptions)
            throughputQ = throughputQ.Where(x =>
                (x.ProcessingResult ?? "").ToLower() == "rejected"
                || (x.RejectionReason != null && x.RejectionReason != ""));

        var throughputRows = await throughputQ
            .Select(x => new
            {
                x.OccurredAt,
                x.ReceivedAtServer,
                Purpose = (x.ReaderWay.BusinessPurposeKey ?? "").Trim(),
                TargetStatus = (x.ReaderWay.TargetProcessStatus ?? "").Trim(),
                x.ProcessingResult,
                x.RejectionReason
            })
            .ToListAsync(cancellationToken);

        var throughputByPurpose = throughputRows
            .Select(x =>
            {
                var key = string.IsNullOrWhiteSpace(x.Purpose) ? x.TargetStatus : x.Purpose;
                return string.IsNullOrWhiteSpace(key) ? "(Unclassified)" : key;
            })
            .GroupBy(x => x)
            .Select(g => new ThroughputRowViewModel { PurposeKey = g.Key, EventCount = g.Count() })
            .OrderByDescending(x => x.EventCount)
            .Take(20)
            .ToList();

        var throughputByDay = throughputRows
            .GroupBy(x => x.OccurredAt.Date)
            .Select(g => new TimeSeriesCountRowViewModel { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToList();

        var rejectedByDay = throughputRows
            .Where(x => (x.ProcessingResult ?? "").Equals("rejected", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(x.RejectionReason))
            .GroupBy(x => x.OccurredAt.Date)
            .Select(g => new TimeSeriesCountRowViewModel { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToList();

        var topRejectionReasons = throughputRows
            .Where(x => !string.IsNullOrWhiteSpace(x.RejectionReason))
            .GroupBy(x => x.RejectionReason!)
            .Select(g => new ReportsReasonCountRowViewModel { Key = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        var lagSeconds = throughputRows
            .Select(x => (int)Math.Max(0, (x.ReceivedAtServer - x.OccurredAt).TotalSeconds))
            .OrderBy(x => x)
            .ToList();

        var avgLag = lagSeconds.Count == 0 ? 0 : (int)Math.Round(lagSeconds.Average());
        var p95Lag = lagSeconds.Count == 0 ? 0 : lagSeconds[(int)Math.Floor(0.95 * (lagSeconds.Count - 1))];

        var damagedByCustomer = await itemsQ
            .Where(x => (x.PhysicalCondition ?? "").ToLower() != "good")
            .GroupBy(x => x.OwnerCustomer != null ? x.OwnerCustomer.CustomerName : "(No owner / pool)")
            .Select(g => new ReportsReasonCountRowViewModel { Key = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(cancellationToken);

        var damagedByLocationRaw = await itemsQ
            .Where(x => (x.PhysicalCondition ?? "").ToLower() != "good")
            .GroupBy(x => x.CurrentLocationId)
            .Select(g => new { LocationId = g.Key, C = g.Count() })
            .ToListAsync(cancellationToken);

        var damagedByLocation = damagedByLocationRaw
            .Select(x => new ReportsReasonCountRowViewModel
            {
                Key = x.LocationId.HasValue && locationNameMap.TryGetValue(x.LocationId.Value, out var name)
                    ? name
                    : x.LocationId.HasValue ? "Unknown location id" : "(Not set)",
                Count = x.C
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        var jobsQ = _dbContext.LogisticsJobs.AsNoTracking().Where(x => x.CustomerId == tenantCustomerId);
        if (customerId.HasValue)
            jobsQ = jobsQ.Where(x => x.CustomerId == customerId.Value);
        if (readerWayId.HasValue)
            jobsQ = jobsQ.Where(x => x.ReaderWayId == readerWayId.Value);

        var jobsByStatus = await jobsQ
            .GroupBy(x => x.JobStatus)
            .Select(g => new JobStatusCountRowViewModel { JobStatus = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        var openJobs = await jobsQ.CountAsync(x => x.JobStatus == "open", cancellationToken);
        var overdueJobs = await jobsQ.CountAsync(x => (x.JobStatus == "open" || x.JobStatus == "in_progress") && x.PlannedEndAt.HasValue && x.PlannedEndAt.Value < now, cancellationToken);
        var stalledCutoff = now.AddHours(-24);
        var stalledJobs = await jobsQ.CountAsync(x => (x.JobStatus == "open" || x.JobStatus == "in_progress")
            && (!_dbContext.LinenMovementEvents.Any(e => e.LogisticsJobId == x.Id)
                || _dbContext.LinenMovementEvents.Where(e => e.LogisticsJobId == x.Id).Max(e => (DateTime?)e.OccurredAt) < stalledCutoff), cancellationToken);

        var viewModel = new ReportsIndexViewModel
        {
            TotalLinenItems = total,
            DamagedItemCount = damaged,
            TotalScansInWindow = throughputRows.Count,
            RejectedScansInWindow = throughputRows.Count(x => (x.ProcessingResult ?? "").Equals("rejected", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(x.RejectionReason)),
            AvgIngestLagSeconds = avgLag,
            P95IngestLagSeconds = p95Lag,
            OpenJobsCount = openJobs,
            OverdueJobsCount = overdueJobs,
            StalledJobsCount = stalledJobs,
            ItemsCleanedByCustomer = cleanRows,
            ItemsByStatus = itemsByStatus,
            ItemsByLocation = itemsByLocation,
            ThroughputByPurpose = throughputByPurpose,
            ThroughputByDay = throughputByDay,
            RejectedByDay = rejectedByDay,
            TopRejectionReasons = topRejectionReasons,
            DamagedByCustomer = damagedByCustomer,
            DamagedByLocation = damagedByLocation,
            JobsByStatus = jobsByStatus,
            Customers = customers,
            Readers = readers,
            ReaderWays = readerWays,
            Query = new ReportsQueryViewModel
            {
                CustomerId = customerId,
                ReaderId = readerId,
                ReaderWayId = readerWayId,
                From = fromUtc,
                To = toUtc,
                OnlyExceptions = onlyExceptions
            },
            WindowNotice = windowNotice,
            GeneratedAtUtc = now
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> ExportSummaryCsv(
        ulong? customerId,
        ulong? readerId,
        ulong? readerWayId,
        bool onlyExceptions = false,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentCustomerId(out var tenantCustomerId))
            return Forbid();
        customerId = tenantCustomerId;

        var now = DateTime.UtcNow;
        var (fromUtc, toUtc, _) = NormalizeUtcWindow(from, to, now);

        var throughputQ = _dbContext.LinenMovementEvents.AsNoTracking()
            .Where(x => x.CustomerId == tenantCustomerId)
            .Where(x => x.OccurredAt >= fromUtc && x.OccurredAt <= toUtc);
        if (customerId.HasValue)
            throughputQ = throughputQ.Where(x => x.LinenItem.OwnerCustomerId == customerId.Value);
        if (readerId.HasValue)
            throughputQ = throughputQ.Where(x => x.ReaderId == readerId.Value);
        if (readerWayId.HasValue)
            throughputQ = throughputQ.Where(x => x.ReaderWayId == readerWayId.Value);
        if (onlyExceptions)
            throughputQ = throughputQ.Where(x =>
                (x.ProcessingResult ?? "").ToLower() == "rejected"
                || (x.RejectionReason != null && x.RejectionReason != ""));

        var rows = await throughputQ
            .OrderByDescending(x => x.OccurredAt)
            .Take(20000)
            .Select(x => new
            {
                x.Id,
                Tag = x.LinenItem.RfidTag,
                Reader = x.Reader.ReaderName,
                Route = x.ReaderWay.WayName,
                Purpose = x.ReaderWay.BusinessPurposeKey,
                TargetStatus = x.ReaderWay.TargetProcessStatus,
                x.ProcessingResult,
                x.RejectionReason,
                x.OccurredAt,
                x.ReceivedAtServer
            })
            .ToListAsync(cancellationToken);

        static string Csv(string? v)
        {
            v ??= "";
            var s = v.Replace("\r", " ").Replace("\n", " ");
            return '"' + s.Replace("\"", "\"\"") + '"';
        }

        var sb = new StringBuilder();
        sb.AppendLine("event_id,tag,reader,scan_route,purpose,target_status,result,rejection_reason,occurred_at,received_at,lag_seconds");
        foreach (var r in rows)
        {
            var lag = (int)Math.Max(0, (r.ReceivedAtServer - r.OccurredAt).TotalSeconds);
            sb.Append(r.Id).Append(',')
                .Append(Csv(r.Tag)).Append(',')
                .Append(Csv(r.Reader)).Append(',')
                .Append(Csv(r.Route)).Append(',')
                .Append(Csv(r.Purpose)).Append(',')
                .Append(Csv(r.TargetStatus)).Append(',')
                .Append(Csv(r.ProcessingResult)).Append(',')
                .Append(Csv(r.RejectionReason)).Append(',')
                .Append(Csv(r.OccurredAt.ToString("u"))).Append(',')
                .Append(Csv(r.ReceivedAtServer.ToString("u"))).Append(',')
                .Append(lag)
                .AppendLine();
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"reports_summary_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportExceptionsCsv(
        ulong? customerId,
        ulong? readerId,
        ulong? readerWayId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentCustomerId(out var tenantCustomerId))
            return Forbid();
        customerId = tenantCustomerId;

        var now = DateTime.UtcNow;
        var (fromUtc, toUtc, _) = NormalizeUtcWindow(from, to, now);

        var query = _dbContext.LinenMovementEvents.AsNoTracking()
            .Where(x => x.CustomerId == tenantCustomerId)
            .Where(x => x.OccurredAt >= fromUtc && x.OccurredAt <= toUtc)
            .Where(x => (x.ProcessingResult ?? "").ToLower() == "rejected" || (x.RejectionReason != null && x.RejectionReason != ""));

        if (customerId.HasValue)
            query = query.Where(x => x.LinenItem.OwnerCustomerId == customerId.Value);
        if (readerId.HasValue)
            query = query.Where(x => x.ReaderId == readerId.Value);
        if (readerWayId.HasValue)
            query = query.Where(x => x.ReaderWayId == readerWayId.Value);

        var rows = await query
            .OrderByDescending(x => x.OccurredAt)
            .Take(20000)
            .Select(x => new
            {
                x.Id,
                Tag = x.LinenItem.RfidTag,
                Customer = x.LinenItem.OwnerCustomer != null ? x.LinenItem.OwnerCustomer.CustomerName : null,
                Reader = x.Reader.ReaderName,
                Route = x.ReaderWay.WayName,
                x.ProcessingResult,
                x.RejectionReason,
                x.OccurredAt
            })
            .ToListAsync(cancellationToken);

        static string Csv(string? v)
        {
            v ??= "";
            var s = v.Replace("\r", " ").Replace("\n", " ");
            return '"' + s.Replace("\"", "\"\"") + '"';
        }

        var sb = new StringBuilder();
        sb.AppendLine("event_id,tag,customer,reader,scan_route,result,rejection_reason,occurred_at");
        foreach (var r in rows)
        {
            sb.Append(r.Id).Append(',')
                .Append(Csv(r.Tag)).Append(',')
                .Append(Csv(r.Customer)).Append(',')
                .Append(Csv(r.Reader)).Append(',')
                .Append(Csv(r.Route)).Append(',')
                .Append(Csv(r.ProcessingResult)).Append(',')
                .Append(Csv(r.RejectionReason)).Append(',')
                .Append(Csv(r.OccurredAt.ToString("u")))
                .AppendLine();
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"reports_exceptions_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
    }

    private static (DateTime fromUtc, DateTime toUtc, string? notice) NormalizeUtcWindow(DateTime? from, DateTime? to, DateTime nowUtc)
    {
        var fromUtc = ToUtcOrDefault(from, nowUtc.AddDays(-7));
        var toUtc = ToUtcOrDefault(to, nowUtc);
        string? notice = null;

        if (toUtc < fromUtc)
        {
            (fromUtc, toUtc) = (toUtc, fromUtc);
            notice = "Date window was auto-corrected because 'From' was later than 'To'.";
        }

        var maxSpan = TimeSpan.FromDays(MaxWindowDays);
        if (toUtc - fromUtc > maxSpan)
        {
            fromUtc = toUtc.AddDays(-MaxWindowDays);
            notice = $"Date window was limited to the last {MaxWindowDays} days for reporting performance.";
        }

        return (fromUtc, toUtc, notice);
    }

    private static DateTime ToUtcOrDefault(DateTime? value, DateTime fallbackUtc)
    {
        if (!value.HasValue)
            return fallbackUtc;

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }

}
