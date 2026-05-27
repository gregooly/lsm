using LaundryMS.Web.Data;
using LaundryMS.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Services;

public sealed class FixedReaderIngestRequest
{
    public ulong CustomerId { get; init; }

    public ulong ReaderId { get; init; }

    public ulong ReaderWayId { get; init; }

    public string RouteTargetStatus { get; init; } = string.Empty;

    public ulong? ReaderWayToLocationId { get; init; }

    public IReadOnlyList<FixedReaderTagEvent> Events { get; init; } = [];

    public int IdempotencyWindowMs { get; init; } = 1000;
}

public sealed class FixedReaderTagEvent
{
    public string IdempotencyKey { get; init; } = string.Empty;

    public string RfidTag { get; init; } = string.Empty;

    public DateTime OccurredAt { get; init; }

    public string? ConditionAfterEvent { get; init; }
}

public sealed class FixedReaderIngestBatchResult
{
    public bool Ok { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<FixedReaderEventResultDto> Results { get; init; } = [];
}

public sealed class FixedReaderEventResultDto
{
    public string? IdempotencyKey { get; init; }
    public string Status { get; init; } = string.Empty;
    public ulong? EventId { get; init; }
    public string? ProcessingResult { get; init; }
    public string? Reason { get; init; }
}

public interface IFixedReaderIngestService
{
    Task<FixedReaderIngestBatchResult> ProcessBatchAsync(FixedReaderIngestRequest request, CancellationToken cancellationToken);
}

public sealed class FixedReaderIngestService : IFixedReaderIngestService
{
    private readonly LaundryMsDbContext _db;
    private readonly ILinenEmailReportService _emailReports;

    public FixedReaderIngestService(LaundryMsDbContext db, ILinenEmailReportService emailReports)
    {
        _db = db;
        _emailReports = emailReports;
    }

    public async Task<FixedReaderIngestBatchResult> ProcessBatchAsync(FixedReaderIngestRequest request, CancellationToken cancellationToken)
    {
        var results = new List<FixedReaderEventResultDto>();
        var now = DateTime.UtcNow;

        var reader = await _db.Readers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ReaderId && x.CustomerId == request.CustomerId && x.IsActive, cancellationToken);

        var routeTargetStatus = request.RouteTargetStatus.Trim().ToLowerInvariant();
        var batchKeys = new HashSet<string>(StringComparer.Ordinal);

        var arrivalAcceptedTypes = new List<string>();
        DateTime? arrivalLatestOccurredUtc = null;

        foreach (var ev in request.Events)
        {
            var key = (ev.IdempotencyKey ?? string.Empty).Trim();
            if (key.Length > 64)
                key = key[..64];

            if (string.IsNullOrEmpty(key))
            {
                results.Add(new FixedReaderEventResultDto
                {
                    IdempotencyKey = ev.IdempotencyKey,
                    Status = "rejected",
                    Reason = "Missing idempotency key."
                });
                continue;
            }

            if (!batchKeys.Add(key))
            {
                results.Add(new FixedReaderEventResultDto
                {
                    IdempotencyKey = key,
                    Status = "duplicate",
                    Reason = "Duplicate idempotency key in this batch."
                });
                continue;
            }

            var duplicate = await _db.LinenMovementEvents.AsNoTracking()
                .AnyAsync(x => x.CustomerId == request.CustomerId && x.IdempotencyKey == key, cancellationToken);
            if (duplicate)
            {
                var existing = await _db.LinenMovementEvents.AsNoTracking()
                    .Where(x => x.CustomerId == request.CustomerId && x.IdempotencyKey == key)
                    .Select(x => new { x.Id, x.ProcessingResult })
                    .FirstAsync(cancellationToken);

                results.Add(new FixedReaderEventResultDto
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
                results.Add(new FixedReaderEventResultDto { IdempotencyKey = key, Status = "rejected", Reason = "Missing RFID tag." });
                continue;
            }

            var linenItem = await _db.LinenItems.FirstOrDefaultAsync(
                x => x.CustomerId == request.CustomerId && x.RfidTag == tag && x.IsActive,
                cancellationToken);

            if (linenItem == null)
            {
                results.Add(new FixedReaderEventResultDto
                {
                    IdempotencyKey = key,
                    Status = "rejected",
                    Reason = "Unknown or inactive RFID tag for this tenant."
                });
                continue;
            }

            var occurredAt = ev.OccurredAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(ev.OccurredAt, DateTimeKind.Utc)
                : ev.OccurredAt.ToUniversalTime();

            var movement = new LinenMovementEvent
            {
                CustomerId = request.CustomerId,
                LinenItemId = linenItem.Id,
                ReaderId = request.ReaderId,
                ReaderWayId = request.ReaderWayId,
                LogisticsJobId = null,
                DriverId = null,
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
                linenItem.CurrentProcessStatus = routeTargetStatus;

            linenItem.LastScannedAt = occurredAt;
            if (request.ReaderWayToLocationId.HasValue)
                linenItem.CurrentLocationId = request.ReaderWayToLocationId;

            if (!string.IsNullOrWhiteSpace(ev.ConditionAfterEvent))
            {
                var c = ev.ConditionAfterEvent.Trim().ToLowerInvariant();
                if (c is "good" or "damaged" or "lost")
                    linenItem.PhysicalCondition = c;
            }

            linenItem.UpdatedAt = now;

            await _db.SaveChangesAsync(cancellationToken);

            arrivalAcceptedTypes.Add(string.IsNullOrWhiteSpace(linenItem.ItemType) ? "item" : linenItem.ItemType.Trim());
            if (!arrivalLatestOccurredUtc.HasValue || occurredAt > arrivalLatestOccurredUtc.Value)
                arrivalLatestOccurredUtc = occurredAt;

            results.Add(new FixedReaderEventResultDto
            {
                IdempotencyKey = key,
                Status = "accepted",
                EventId = movement.Id,
                ProcessingResult = movement.ProcessingResult
            });
        }

        var ok = results.TrueForAll(r => r.Status is "accepted" or "duplicate");

        if (reader != null && arrivalAcceptedTypes.Count > 0
                              && LinenEmailReportTriggers.ShouldSendArrivalEmail(routeTargetStatus, reader.ReaderCategory))
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in arrivalAcceptedTypes)
            {
                counts.TryGetValue(t, out var n);
                counts[t] = n + 1;
            }

            await _emailReports.TrySendArrivalReportAsync(
                request.CustomerId,
                counts,
                arrivalLatestOccurredUtc ?? now,
                cancellationToken).ConfigureAwait(false);
        }

        return new FixedReaderIngestBatchResult
        {
            Ok = ok,
            Message = ok ? "Processed." : "Completed with rejections.",
            Results = results
        };
    }
}
