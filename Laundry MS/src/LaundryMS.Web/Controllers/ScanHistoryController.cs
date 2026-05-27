using System.Text;
using LaundryMS.Web.Data;
using LaundryMS.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Controllers;

public class ScanHistoryController : TenantScopedController
{
    private readonly LaundryMsDbContext _dbContext;

    public ScanHistoryController(LaundryMsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(
        string? tag,
        string? q,
        ulong? readerId,
        ulong? readerWayId,
        string? processingResult,
        ulong? jobId,
        string? rejectionReason,
        string? idempotencyKey,
        bool onlyRejected = false,
        bool onlyDuplicates = false,
        int minIngestLagSeconds = 0,
        DateTime? from = null,
        DateTime? to = null,
        string sortBy = "occurred",
        string sortDir = "desc",
        int page = 1,
        int pageSize = 200,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 50 or > 500 ? 200 : pageSize;
        minIngestLagSeconds = minIngestLagSeconds is < 0 or > 86400 ? 0 : minIngestLagSeconds;

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            (from, to) = (to, from);
        }

        var baseQuery = _dbContext.LinenMovementEvents.AsNoTracking().Where(x => x.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var t = tag.Trim();
            baseQuery = baseQuery.Where(x => x.LinenItem.RfidTag.Contains(t));
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            baseQuery = baseQuery.Where(x =>
                x.LinenItem.RfidTag.Contains(term)
                || (x.IdempotencyKey != null && x.IdempotencyKey.Contains(term))
                || (x.RejectionReason != null && x.RejectionReason.Contains(term)));
        }

        if (readerId.HasValue)
            baseQuery = baseQuery.Where(x => x.ReaderId == readerId.Value);

        if (readerWayId.HasValue)
            baseQuery = baseQuery.Where(x => x.ReaderWayId == readerWayId.Value);

        if (!string.IsNullOrWhiteSpace(processingResult))
        {
            var r = processingResult.Trim().ToLowerInvariant();
            baseQuery = baseQuery.Where(x => (x.ProcessingResult ?? "").ToLower() == r);
        }

        if (jobId.HasValue)
            baseQuery = baseQuery.Where(x => x.LogisticsJobId == jobId.Value);

        if (!string.IsNullOrWhiteSpace(rejectionReason))
        {
            var rr = rejectionReason.Trim();
            baseQuery = baseQuery.Where(x => x.RejectionReason != null && x.RejectionReason.Contains(rr));
        }

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var key = idempotencyKey.Trim();
            baseQuery = baseQuery.Where(x => x.IdempotencyKey == key);
        }

        if (onlyRejected)
        {
            baseQuery = baseQuery.Where(x =>
                (x.ProcessingResult ?? "").ToLower() == "rejected"
                || x.RejectionReason != null);
        }

        if (minIngestLagSeconds > 0)
        {
            baseQuery = baseQuery.Where(x => x.ReceivedAtServer >= x.OccurredAt.AddSeconds(minIngestLagSeconds));
        }

        if (from.HasValue)
            baseQuery = baseQuery.Where(x => x.OccurredAt >= from.Value);

        if (to.HasValue)
            baseQuery = baseQuery.Where(x => x.OccurredAt <= to.Value);

        if (onlyDuplicates)
        {
            var duplicateKeys = baseQuery
                .Where(x => x.IdempotencyKey != null && x.IdempotencyKey != "")
                .GroupBy(x => x.IdempotencyKey)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            baseQuery = baseQuery.Where(x => x.IdempotencyKey != null && duplicateKeys.Contains(x.IdempotencyKey));
        }

        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "occurred" : sortBy.Trim().ToLowerInvariant();
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        baseQuery = sortBy switch
        {
            "received" => desc
                ? baseQuery.OrderByDescending(x => x.ReceivedAtServer).ThenByDescending(x => x.OccurredAt)
                : baseQuery.OrderBy(x => x.ReceivedAtServer).ThenByDescending(x => x.OccurredAt),
            _ => desc
                ? baseQuery.OrderByDescending(x => x.OccurredAt)
                : baseQuery.OrderBy(x => x.OccurredAt)
        };

        var total = await baseQuery.CountAsync(cancellationToken);

        var rows = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.LinenItemId,
                x.LinenItem.RfidTag,
                x.ReaderId,
                ReaderName = x.Reader.ReaderName,
                x.ReaderWayId,
                WayName = x.ReaderWay.WayName,
                x.ProcessingResult,
                x.LogisticsJobId,
                x.RejectionReason,
                x.ConditionAfterEvent,
                x.IdempotencyKey,
                x.OccurredAt,
                x.ReceivedAtServer
            })
            .ToListAsync(cancellationToken);

        var events = rows.Select(x => new MovementEventListItemViewModel
        {
            Id = x.Id,
            LinenItemId = x.LinenItemId,
            RfidTag = x.RfidTag,
            ReaderId = x.ReaderId,
            ReaderName = x.ReaderName,
            ReaderWayId = x.ReaderWayId,
            WayName = x.WayName,
            ProcessingResult = x.ProcessingResult,
            LogisticsJobId = x.LogisticsJobId,
            RejectionReason = x.RejectionReason,
            ConditionAfterEvent = x.ConditionAfterEvent,
            IdempotencyKey = x.IdempotencyKey,
            OccurredAt = x.OccurredAt,
            ReceivedAtServer = x.ReceivedAtServer,
            IngestLagSeconds = (int)Math.Max(0, (x.ReceivedAtServer - x.OccurredAt).TotalSeconds)
        }).ToList();

        var readers = await _dbContext.Readers
            .AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == customerId)
            .OrderBy(x => x.ReaderName)
            .Select(x => new ReaderOptionViewModel { Id = x.Id, ReaderName = x.ReaderName })
            .ToListAsync(cancellationToken);

        var ways = await _dbContext.ReaderWays
            .AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == customerId)
            .OrderBy(x => x.WayName)
            .Select(x => new ReaderWayOptionViewModel
            {
                Id = x.Id,
                DisplayName = x.WayName + " — " + x.Reader.ReaderName
            })
            .ToListAsync(cancellationToken);

        var resultOptions = await _dbContext.LinenMovementEvents
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .Select(x => x.ProcessingResult)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        // KPIs for last 24h (unfiltered, global ops signal)
        var since24 = DateTime.UtcNow.AddHours(-24);
        var q24 = _dbContext.LinenMovementEvents.AsNoTracking().Where(x => x.CustomerId == customerId && x.OccurredAt >= since24);

        var total24 = await q24.CountAsync(cancellationToken);
        var rejected24 = await q24.CountAsync(x => (x.ProcessingResult ?? "").ToLower() == "rejected" || x.RejectionReason != null, cancellationToken);

        var dupKeys24 = await q24
            .Where(x => x.IdempotencyKey != null && x.IdempotencyKey != "")
            .GroupBy(x => x.IdempotencyKey)
            .Where(g => g.Count() > 1)
            .CountAsync(cancellationToken);

        var lagRows24 = await q24
            .Select(x => new { x.OccurredAt, x.ReceivedAtServer })
            .ToListAsync(cancellationToken);
        var lags = lagRows24
            .Select(x => (int)Math.Max(0, (x.ReceivedAtServer - x.OccurredAt).TotalSeconds))
            .OrderBy(x => x)
            .ToList();
        var avgLag = lags.Count == 0 ? 0 : (int)Math.Round(lags.Average());
        var p95Lag = lags.Count == 0 ? 0 : lags[(int)Math.Floor(0.95 * (lags.Count - 1))];

        var topReasons = await q24
            .Where(x => x.RejectionReason != null && x.RejectionReason != "")
            .GroupBy(x => x.RejectionReason!)
            .Select(g => new { Reason = g.Key, C = g.Count() })
            .OrderByDescending(x => x.C)
            .Take(5)
            .ToListAsync(cancellationToken);

        return View(new ScanHistoryIndexViewModel
        {
            Events = events,
            Readers = readers,
            ReaderWays = ways,
            ProcessingResultOptions = resultOptions,
            Query = new ScanHistoryQueryViewModel
            {
                Tag = string.IsNullOrWhiteSpace(tag) ? null : tag.Trim(),
                Q = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
                ReaderId = readerId,
                ReaderWayId = readerWayId,
                ProcessingResult = string.IsNullOrWhiteSpace(processingResult) ? null : processingResult.Trim().ToLowerInvariant(),
                JobId = jobId,
                RejectionReason = string.IsNullOrWhiteSpace(rejectionReason) ? null : rejectionReason.Trim(),
                IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim(),
                OnlyRejected = onlyRejected,
                OnlyDuplicates = onlyDuplicates,
                MinIngestLagSeconds = minIngestLagSeconds,
                From = from,
                To = to,
                SortBy = sortBy,
                SortDir = desc ? "desc" : "asc"
            },
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Total24h = total24,
            Rejected24h = rejected24,
            Duplicates24h = dupKeys24,
            AvgLagSeconds24h = avgLag,
            P95LagSeconds24h = p95Lag,
            TopRejectionReasons24h = topReasons.Select(x => new ReasonCountRowViewModel { Reason = x.Reason, Count = x.C }).ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(
        string? tag,
        string? q,
        ulong? readerId,
        ulong? readerWayId,
        string? processingResult,
        ulong? jobId,
        string? rejectionReason,
        string? idempotencyKey,
        bool onlyRejected = false,
        bool onlyDuplicates = false,
        int minIngestLagSeconds = 0,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        minIngestLagSeconds = minIngestLagSeconds is < 0 or > 86400 ? 0 : minIngestLagSeconds;

        var query = _dbContext.LinenMovementEvents.AsNoTracking().Where(x => x.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var t = tag.Trim();
            query = query.Where(x => x.LinenItem.RfidTag.Contains(t));
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.LinenItem.RfidTag.Contains(term)
                || (x.IdempotencyKey != null && x.IdempotencyKey.Contains(term))
                || (x.RejectionReason != null && x.RejectionReason.Contains(term)));
        }

        if (readerId.HasValue)
            query = query.Where(x => x.ReaderId == readerId.Value);

        if (readerWayId.HasValue)
            query = query.Where(x => x.ReaderWayId == readerWayId.Value);

        if (!string.IsNullOrWhiteSpace(processingResult))
        {
            var r = processingResult.Trim().ToLowerInvariant();
            query = query.Where(x => (x.ProcessingResult ?? "").ToLower() == r);
        }

        if (jobId.HasValue)
            query = query.Where(x => x.LogisticsJobId == jobId.Value);

        if (!string.IsNullOrWhiteSpace(rejectionReason))
        {
            var rr = rejectionReason.Trim();
            query = query.Where(x => x.RejectionReason != null && x.RejectionReason.Contains(rr));
        }

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var key = idempotencyKey.Trim();
            query = query.Where(x => x.IdempotencyKey == key);
        }

        if (onlyRejected)
            query = query.Where(x => (x.ProcessingResult ?? "").ToLower() == "rejected" || x.RejectionReason != null);

        if (minIngestLagSeconds > 0)
            query = query.Where(x => x.ReceivedAtServer >= x.OccurredAt.AddSeconds(minIngestLagSeconds));

        if (from.HasValue)
            query = query.Where(x => x.OccurredAt >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.OccurredAt <= to.Value);

        if (onlyDuplicates)
        {
            var duplicateKeys = query
                .Where(x => x.IdempotencyKey != null && x.IdempotencyKey != "")
                .GroupBy(x => x.IdempotencyKey)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            query = query.Where(x => x.IdempotencyKey != null && duplicateKeys.Contains(x.IdempotencyKey));
        }

        var rows = await query
            .OrderByDescending(x => x.OccurredAt)
            .Take(20000)
            .Select(x => new
            {
                x.Id,
                x.LinenItem.RfidTag,
                ReaderName = x.Reader.ReaderName,
                WayName = x.ReaderWay.WayName,
                x.ProcessingResult,
                x.LogisticsJobId,
                x.RejectionReason,
                x.IdempotencyKey,
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
        sb.AppendLine("event_id,rfid_tag,reader,scan_route,result,job_id,rejection_reason,idempotency_key,occurred_at,received_at,lag_seconds");
        foreach (var r in rows)
        {
            var lag = (int)Math.Max(0, (r.ReceivedAtServer - r.OccurredAt).TotalSeconds);
            sb.Append(r.Id).Append(',')
                .Append(Csv(r.RfidTag)).Append(',')
                .Append(Csv(r.ReaderName)).Append(',')
                .Append(Csv(r.WayName)).Append(',')
                .Append(Csv(r.ProcessingResult)).Append(',')
                .Append(r.LogisticsJobId?.ToString() ?? "").Append(',')
                .Append(Csv(r.RejectionReason)).Append(',')
                .Append(Csv(r.IdempotencyKey)).Append(',')
                .Append(Csv(r.OccurredAt.ToString("u"))).Append(',')
                .Append(Csv(r.ReceivedAtServer.ToString("u"))).Append(',')
                .Append(lag)
                .AppendLine();
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"scan_history_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Get(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var row = await _dbContext.LinenMovementEvents
            .AsNoTracking()
            .Where(x => x.Id == id && x.CustomerId == customerId)
            .Select(x => new
            {
                x.Id,
                x.LinenItemId,
                x.LinenItem.RfidTag,
                x.ReaderId,
                ReaderName = x.Reader.ReaderName,
                x.ReaderWayId,
                WayName = x.ReaderWay.WayName,
                x.ProcessingResult,
                x.LogisticsJobId,
                x.RejectionReason,
                x.ConditionAfterEvent,
                x.IdempotencyKey,
                x.OccurredAt,
                x.ReceivedAtServer
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row == null) return Json(new { ok = false, message = "Event not found." });

        var ev = new MovementEventListItemViewModel
        {
            Id = row.Id,
            LinenItemId = row.LinenItemId,
            RfidTag = row.RfidTag,
            ReaderId = row.ReaderId,
            ReaderName = row.ReaderName,
            ReaderWayId = row.ReaderWayId,
            WayName = row.WayName,
            ProcessingResult = row.ProcessingResult,
            LogisticsJobId = row.LogisticsJobId,
            RejectionReason = row.RejectionReason,
            ConditionAfterEvent = row.ConditionAfterEvent,
            IdempotencyKey = row.IdempotencyKey,
            OccurredAt = row.OccurredAt,
            ReceivedAtServer = row.ReceivedAtServer,
            IngestLagSeconds = (int)Math.Max(0, (row.ReceivedAtServer - row.OccurredAt).TotalSeconds)
        };

        return Json(new { ok = true, ev });
    }
}
