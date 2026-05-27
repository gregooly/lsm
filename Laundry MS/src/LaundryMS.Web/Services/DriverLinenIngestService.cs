using LaundryMS.Web.Data;
using LaundryMS.Web.Models.Api;
using LaundryMS.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Services;

public class DriverLinenIngestService
{
    private readonly LaundryMsDbContext _db;
    private readonly ILinenEmailReportService _emailReports;

    public DriverLinenIngestService(LaundryMsDbContext db, ILinenEmailReportService emailReports)
    {
        _db = db;
        _emailReports = emailReports;
    }

    public async Task<LinenMovementBatchResult> ProcessBatchAsync(
        LinenMovementBatchRequest request,
        ulong tenantCustomerId,
        ulong driverId,
        CancellationToken cancellationToken)
    {
        var results = new List<MovementEventResultDto>();
        var now = DateTime.UtcNow;

        if (request.CustomerId != tenantCustomerId || request.DriverId != driverId)
        {
            return new LinenMovementBatchResult
            {
                Ok = false,
                Message = "Token does not match request driver or customer.",
                Results = results
            };
        }

        var reader = await _db.Readers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ReaderId && x.CustomerId == tenantCustomerId && x.IsActive, cancellationToken);
        if (reader == null)
        {
            return new LinenMovementBatchResult { Ok = false, Message = "Reader not found or inactive.", Results = results };
        }

        var readerWay = await _db.ReaderWays.AsNoTracking()
            .Include(w => w.FromLocation)
            .Include(w => w.ToLocation)
            .FirstOrDefaultAsync(x => x.Id == request.ReaderWayId && x.CustomerId == tenantCustomerId && x.IsActive, cancellationToken);
        if (readerWay == null)
        {
            return new LinenMovementBatchResult { Ok = false, Message = "Scan route not found or inactive.", Results = results };
        }

        if (readerWay.ReaderId != request.ReaderId)
        {
            return new LinenMovementBatchResult { Ok = false, Message = "Scan route does not belong to this reader.", Results = results };
        }

        var routeTargetStatus = (readerWay.TargetProcessStatus ?? string.Empty).Trim().ToLowerInvariant();

        LogisticsJob? job = null;
        if (request.JobId is { } jobId && jobId != 0)
        {
            job = await _db.LogisticsJobs.FirstOrDefaultAsync(x => x.Id == jobId && x.CustomerId == tenantCustomerId, cancellationToken);
            if (job == null)
            {
                return new LinenMovementBatchResult { Ok = false, Message = "Logistics job not found.", Results = results };
            }

            if (job.DriverId.HasValue && job.DriverId.Value != driverId)
            {
                return new LinenMovementBatchResult { Ok = false, Message = "Job is assigned to another driver.", Results = results };
            }

            if (job.ReaderWayId.HasValue && job.ReaderWayId.Value != request.ReaderWayId)
            {
                return new LinenMovementBatchResult { Ok = false, Message = "Job scan route does not match request.", Results = results };
            }
        }

        var batchIdempotencyKeys = new HashSet<string>(StringComparer.Ordinal);
        var pickupAcceptedTypes = new List<string>();
        DateTime? pickupLatestOccurredUtc = null;

        foreach (var ev in request.Events)
        {
            var key = (ev.IdempotencyKey ?? string.Empty).Trim();
            if (key.Length > 64)
                key = key[..64];

            if (string.IsNullOrEmpty(key))
            {
                results.Add(new MovementEventResultDto
                {
                    IdempotencyKey = ev.IdempotencyKey,
                    Status = "rejected",
                    Reason = "Missing idempotency key."
                });
                continue;
            }

            if (!batchIdempotencyKeys.Add(key))
            {
                results.Add(new MovementEventResultDto
                {
                    IdempotencyKey = key,
                    Status = "duplicate",
                    Reason = "Duplicate idempotency key in this batch."
                });
                continue;
            }

            if (ev.DriverId is { } evDriver && evDriver != 0 && evDriver != driverId)
            {
                results.Add(new MovementEventResultDto
                {
                    IdempotencyKey = key,
                    Status = "rejected",
                    Reason = "Event driver id does not match authenticated driver."
                });
                continue;
            }

            var duplicate = await _db.LinenMovementEvents.AsNoTracking()
                .AnyAsync(x => x.CustomerId == tenantCustomerId && x.IdempotencyKey == key, cancellationToken);
            if (duplicate)
            {
                var existing = await _db.LinenMovementEvents.AsNoTracking()
                    .Where(x => x.CustomerId == tenantCustomerId && x.IdempotencyKey == key)
                    .Select(x => new { x.Id, x.ProcessingResult })
                    .FirstAsync(cancellationToken);

                results.Add(new MovementEventResultDto
                {
                    IdempotencyKey = key,
                    Status = "duplicate",
                    EventId = existing.Id,
                    ProcessingResult = existing.ProcessingResult
                });
                continue;
            }

            var tag = (ev.RfidTag ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(tag))
            {
                results.Add(new MovementEventResultDto { IdempotencyKey = key, Status = "rejected", Reason = "Missing RFID tag." });
                continue;
            }

            var linenItem = await _db.LinenItems.FirstOrDefaultAsync(
                x => x.CustomerId == tenantCustomerId && x.RfidTag == tag && x.IsActive,
                cancellationToken);

            if (linenItem == null)
            {
                results.Add(new MovementEventResultDto
                {
                    IdempotencyKey = key,
                    Status = "rejected",
                    Reason = "Unknown or inactive RFID tag for this tenant."
                });
                continue;
            }

            var effectiveJobId = ev.JobId ?? request.JobId;
            if (effectiveJobId is { } ej && ej != 0 && job != null && ej != job.Id)
            {
                results.Add(new MovementEventResultDto
                {
                    IdempotencyKey = key,
                    Status = "rejected",
                    Reason = "Event job id does not match batch job."
                });
                continue;
            }

            var effectiveReaderId = ev.ReaderId ?? request.ReaderId;
            var effectiveWayId = ev.ReaderWayId ?? request.ReaderWayId;
            if (effectiveReaderId != request.ReaderId || effectiveWayId != request.ReaderWayId)
            {
                results.Add(new MovementEventResultDto
                {
                    IdempotencyKey = key,
                    Status = "rejected",
                    Reason = "Per-event reader/route must match batch envelope."
                });
                continue;
            }

            var occurredAt = ev.OccurredAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(ev.OccurredAt, DateTimeKind.Utc)
                : ev.OccurredAt.ToUniversalTime();

            var movement = new LinenMovementEvent
            {
                CustomerId = tenantCustomerId,
                LinenItemId = linenItem.Id,
                ReaderId = request.ReaderId,
                ReaderWayId = request.ReaderWayId,
                LogisticsJobId = job?.Id,
                DriverId = driverId,
                OccurredAt = occurredAt,
                ReceivedAtServer = now,
                IdempotencyKey = key,
                ProcessingResult = "accepted",
                RejectionReason = null,
                ConditionAfterEvent = string.IsNullOrWhiteSpace(ev.ConditionAfterEvent)
                    ? null
                    : ev.ConditionAfterEvent.Trim()[..Math.Min(ev.ConditionAfterEvent.Trim().Length, 24)],
                CreatedAt = now
            };

            _db.LinenMovementEvents.Add(movement);

            if (!string.IsNullOrEmpty(routeTargetStatus))
            {
                linenItem.CurrentProcessStatus = routeTargetStatus;
            }

            linenItem.LastScannedAt = occurredAt;
            if (readerWay.ToLocationId.HasValue)
            {
                linenItem.CurrentLocationId = readerWay.ToLocationId;
            }

            if (!string.IsNullOrWhiteSpace(ev.ConditionAfterEvent))
            {
                var c = ev.ConditionAfterEvent.Trim().ToLowerInvariant();
                if (c is "good" or "damaged" or "lost")
                    linenItem.PhysicalCondition = c;
            }

            linenItem.UpdatedAt = now;

            if (job?.Id is { } jid2 && !string.IsNullOrEmpty(routeTargetStatus))
            {
                var expectedRows = await _db.JobExpectedItems
                    .Where(x => x.CustomerId == tenantCustomerId && x.LogisticsJobId == jid2 && x.LinenItemId == linenItem.Id)
                    .ToListAsync(cancellationToken);

                foreach (var je in expectedRows)
                {
                    if (string.Equals(je.ExpectedProcessStatus.Trim(), routeTargetStatus, StringComparison.OrdinalIgnoreCase))
                    {
                        je.ReachedExpectedStatus = true;
                        je.ReachedAt ??= now;
                    }
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            pickupAcceptedTypes.Add(string.IsNullOrWhiteSpace(linenItem.ItemType) ? "item" : linenItem.ItemType.Trim());
            if (!pickupLatestOccurredUtc.HasValue || occurredAt > pickupLatestOccurredUtc.Value)
                pickupLatestOccurredUtc = occurredAt;

            results.Add(new MovementEventResultDto
            {
                IdempotencyKey = key,
                Status = "accepted",
                EventId = movement.Id,
                ProcessingResult = movement.ProcessingResult
            });
        }

        var ok = results.TrueForAll(r => r.Status is "accepted" or "duplicate");

        if (pickupAcceptedTypes.Count > 0
                 && LinenEmailReportTriggers.ShouldSendPickupEmail(
                     routeTargetStatus,
                     readerWay.BusinessPurposeKey,
                     reader.ReaderCategory))
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in pickupAcceptedTypes)
            {
                counts.TryGetValue(t, out var n);
                counts[t] = n + 1;
            }

            var pickupLocation = string.IsNullOrWhiteSpace(readerWay.FromLocation?.LocationName)
                ? (string.IsNullOrWhiteSpace(readerWay.ToLocation?.LocationName)
                    ? "Not specified"
                    : readerWay.ToLocation!.LocationName.Trim())
                : readerWay.FromLocation!.LocationName.Trim();

            await _emailReports.TrySendPickupReportAsync(
                tenantCustomerId,
                pickupLocation,
                counts,
                pickupLatestOccurredUtc ?? now,
                cancellationToken).ConfigureAwait(false);
        }

        return new LinenMovementBatchResult
        {
            Ok = ok,
            Message = ok ? "Processed." : "Completed with rejections.",
            Results = results
        };
    }
}

public sealed class LinenMovementBatchResult
{
    public bool Ok { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<MovementEventResultDto> Results { get; init; } = [];
}

public sealed class MovementEventResultDto
{
    public string? IdempotencyKey { get; init; }
    public string Status { get; init; } = string.Empty;
    public ulong? EventId { get; init; }
    public string? ProcessingResult { get; init; }
    public string? Reason { get; init; }
}
