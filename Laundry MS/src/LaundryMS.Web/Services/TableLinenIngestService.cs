using LaundryMS.Web.Data;
using LaundryMS.Web.Models.Api;
using LaundryMS.Web.Models.Entities;
using LaundryMS.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Services;

public sealed class TableLinenIngestService
{
    private readonly LaundryMsDbContext _db;

    public TableLinenIngestService(LaundryMsDbContext db)
    {
        _db = db;
    }

    public async Task<LinenMovementBatchResult> ProcessBatchAsync(
        TableLinenMovementBatchRequest request,
        ulong tenantCustomerId,
        ulong tokenDriverId,
        CancellationToken cancellationToken)
    {
        var results = new List<MovementEventResultDto>();
        var now = DateTime.UtcNow;

        if (request.CustomerId != tenantCustomerId)
        {
            return new LinenMovementBatchResult
            {
                Ok = false,
                Message = "Token does not match request customer.",
                Results = results
            };
        }

        if (request.ReaderId == 0)
        {
            return new LinenMovementBatchResult
            {
                Ok = false,
                Message = "readerId is required (store readerId from connection-status).",
                Results = results
            };
        }

        var reader = await _db.Readers.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == request.ReaderId && x.CustomerId == tenantCustomerId && x.IsActive,
                cancellationToken)
            .ConfigureAwait(false);

        if (reader == null)
        {
            return new LinenMovementBatchResult { Ok = false, Message = "Reader not found or inactive.", Results = results };
        }

        var readerWay = await _db.ReaderWays.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == request.ReaderWayId && x.CustomerId == tenantCustomerId && x.IsActive,
                cancellationToken)
            .ConfigureAwait(false);

        if (readerWay == null)
        {
            return new LinenMovementBatchResult { Ok = false, Message = "Scan route not found or inactive.", Results = results };
        }

        if (readerWay.ReaderId != request.ReaderId)
        {
            return new LinenMovementBatchResult { Ok = false, Message = "Scan route does not belong to this reader.", Results = results };
        }

        var routeTargetStatus = (readerWay.TargetProcessStatus ?? string.Empty).Trim().ToLowerInvariant();
        var clearStatuses = await GetTemporaryClearStatusesAsync(tenantCustomerId, cancellationToken).ConfigureAwait(false);
        var reportedBy = $"table:{reader.ReaderName}";

        var batchIdempotencyKeys = new HashSet<string>(StringComparer.Ordinal);

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

            if (!LinenConditionIngestHelper.TryNormalizeCondition(ev.ConditionAfterEvent, out var condition))
            {
                results.Add(new MovementEventResultDto
                {
                    IdempotencyKey = key,
                    Status = "rejected",
                    Reason = "ConditionAfterEvent must be good, damaged, or lost."
                });
                continue;
            }

            var duplicate = await _db.LinenMovementEvents.AsNoTracking()
                .AnyAsync(x => x.CustomerId == tenantCustomerId && x.IdempotencyKey == key, cancellationToken)
                .ConfigureAwait(false);

            if (duplicate)
            {
                var existing = await _db.LinenMovementEvents.AsNoTracking()
                    .Where(x => x.CustomerId == tenantCustomerId && x.IdempotencyKey == key)
                    .Select(x => new { x.Id, x.ProcessingResult })
                    .FirstAsync(cancellationToken)
                    .ConfigureAwait(false);

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
                    cancellationToken)
                .ConfigureAwait(false);

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

            if (!string.IsNullOrEmpty(routeTargetStatus))
            {
                var transitionErr = LinenProcessTransitions.ValidateTransition(linenItem.CurrentProcessStatus, routeTargetStatus);
                if (transitionErr != null)
                {
                    results.Add(new MovementEventResultDto
                    {
                        IdempotencyKey = key,
                        Status = "rejected",
                        Reason = transitionErr
                    });
                    continue;
                }
            }

            var occurredAt = ev.OccurredAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(ev.OccurredAt, DateTimeKind.Utc)
                : ev.OccurredAt.ToUniversalTime();

            var previousStatus = linenItem.CurrentProcessStatus;

            var movement = new LinenMovementEvent
            {
                CustomerId = tenantCustomerId,
                LinenItemId = linenItem.Id,
                ReaderId = request.ReaderId,
                ReaderWayId = request.ReaderWayId,
                LogisticsJobId = null,
                DriverId = tokenDriverId,
                OccurredAt = occurredAt,
                ReceivedAtServer = now,
                IdempotencyKey = key,
                ProcessingResult = "accepted",
                RejectionReason = null,
                ConditionAfterEvent = condition,
                CreatedAt = now
            };

            _db.LinenMovementEvents.Add(movement);

            if (!string.IsNullOrEmpty(routeTargetStatus))
                linenItem.CurrentProcessStatus = routeTargetStatus;

            linenItem.LastScannedAt = occurredAt;

            if (readerWay.ToLocationId.HasValue)
                linenItem.CurrentLocationId = readerWay.ToLocationId;

            LinenConditionIngestHelper.ApplyConditionChange(_db, linenItem, tenantCustomerId, condition, reportedBy, now);

            LinenWorkflowHelper.ApplyTemporaryEmployeeClear(linenItem, previousStatus, clearStatuses);

            linenItem.UpdatedAt = now;

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            results.Add(new MovementEventResultDto
            {
                IdempotencyKey = key,
                Status = "accepted",
                EventId = movement.Id,
                ProcessingResult = movement.ProcessingResult
            });
        }

        var ok = results.TrueForAll(r => r.Status is "accepted" or "duplicate");

        return new LinenMovementBatchResult
        {
            Ok = ok,
            Message = ok ? "Processed." : "Completed with rejections.",
            Results = results
        };
    }

    public async Task<TableLinenLookupResponse> LookupByTagAsync(
        ulong tenantCustomerId,
        ulong readerId,
        string rfidTag,
        ulong? readerWayId,
        CancellationToken cancellationToken)
    {
        var tag = (rfidTag ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(tag))
        {
            return new TableLinenLookupResponse
            {
                Found = false,
                Warnings = ["RFID tag is required."]
            };
        }

        var row = await _db.LinenItems.AsNoTracking()
            .Where(x => x.CustomerId == tenantCustomerId && x.RfidTag == tag)
            .Select(x => new
            {
                x.Id,
                x.RfidTag,
                x.ItemType,
                x.SizeLabel,
                x.DefaultAssignmentType,
                x.OwnerCustomerId,
                OwnerCustomerName = x.OwnerCustomer != null ? x.OwnerCustomer.CustomerName : null,
                x.AssignedEmployeeId,
                AssignedEmployeeName = x.AssignedEmployee != null ? x.AssignedEmployee.EmployeeName : null,
                x.CurrentLocationId,
                CurrentLocationName = x.CurrentLocation != null ? x.CurrentLocation.LocationName : null,
                x.CurrentProcessStatus,
                x.PhysicalCondition,
                x.LastScannedAt,
                x.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (row == null || !row.IsActive)
        {
            return new TableLinenLookupResponse
            {
                Found = false,
                Warnings = ["Unknown or inactive RFID tag for this tenant."]
            };
        }

        var warnings = new List<string>();

        if (readerWayId is { } wayId && wayId != 0)
        {
            var way = await _db.ReaderWays.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == wayId && x.CustomerId == tenantCustomerId && x.ReaderId == readerId && x.IsActive,
                    cancellationToken)
                .ConfigureAwait(false);

            if (way == null)
            {
                warnings.Add("Scan route not found for this table reader.");
            }
            else
            {
                var target = (way.TargetProcessStatus ?? string.Empty).Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(target))
                {
                    var err = LinenProcessTransitions.ValidateTransition(row.CurrentProcessStatus, target);
                    if (err != null)
                        warnings.Add(err);
                }
            }
        }

        return new TableLinenLookupResponse
        {
            Found = true,
            Item = new TableLinenItemApiDto
            {
                Id = row.Id,
                RfidTag = row.RfidTag,
                ItemType = row.ItemType,
                SizeLabel = row.SizeLabel,
                DefaultAssignmentType = row.DefaultAssignmentType,
                OwnerCustomerId = row.OwnerCustomerId,
                OwnerCustomerName = row.OwnerCustomerName,
                AssignedEmployeeId = row.AssignedEmployeeId,
                AssignedEmployeeName = row.AssignedEmployeeName,
                CurrentLocationId = row.CurrentLocationId,
                CurrentLocationName = row.CurrentLocationName,
                CurrentProcessStatus = row.CurrentProcessStatus,
                PhysicalCondition = row.PhysicalCondition,
                LastScannedAt = row.LastScannedAt,
                IsActive = row.IsActive
            },
            Warnings = warnings
        };
    }

    private async Task<IReadOnlyList<string>> GetTemporaryClearStatusesAsync(ulong customerId, CancellationToken cancellationToken)
    {
        var row = await _db.SystemSettings.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.CustomerId == customerId && x.SettingKey == SettingsKeys.WorkflowClearTemporaryEmployeeOnStatus,
                cancellationToken)
            .ConfigureAwait(false);

        return LinenWorkflowHelper.ParseStatusList(row?.SettingValue);
    }
}
