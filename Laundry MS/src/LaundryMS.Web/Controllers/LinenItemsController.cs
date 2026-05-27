using LaundryMS.Web.Data;
using LaundryMS.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Controllers;

public class LinenItemsController : TenantScopedController
{
    private const int TimelineTake = 200;

    private readonly LaundryMsDbContext _dbContext;

    public LinenItemsController(LaundryMsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(
        string? q,
        ulong? customerId,
        ulong? locationId,
        string? processStatus,
        string? physicalCondition,
        string? processingResult,
        int? staleDays,
        bool exceptionOnly = false,
        string sortBy = "rfid",
        string sortDir = "asc",
        bool locationUnassigned = false,
        bool includeInactive = false,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentCustomerId(out var tenantCustomerId))
            return Forbid();
        customerId = tenantCustomerId;

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 10 or > 200 ? 50 : pageSize;

        var query = _dbContext.LinenItems.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.RfidTag.Contains(term));
        }

        if (customerId.HasValue)
            query = query.Where(x => x.CustomerId == customerId.Value);

        if (locationUnassigned)
        {
            query = query.Where(x => x.CurrentLocationId == null);
        }
        else if (locationId.HasValue)
            query = query.Where(x => x.CurrentLocationId == locationId.Value);

        if (!string.IsNullOrWhiteSpace(processStatus))
        {
            var ps = NormalizeStatus(processStatus);
            query = query.Where(x => NormalizeStatus(x.CurrentProcessStatus) == ps);
        }

        if (!string.IsNullOrWhiteSpace(physicalCondition))
        {
            var pc = physicalCondition.Trim();
            query = query.Where(x => x.PhysicalCondition == pc);
        }

        if (!string.IsNullOrWhiteSpace(processingResult))
        {
            var pr = processingResult.Trim().ToLowerInvariant();
            query = query.Where(x => _dbContext.LinenMovementEvents
                .Any(e => e.CustomerId == tenantCustomerId && e.LinenItemId == x.Id && e.ProcessingResult.ToLower() == pr));
        }

        if (staleDays.HasValue && staleDays.Value > 0)
        {
            var cutoff = DateTime.UtcNow.AddDays(-staleDays.Value);
            query = query.Where(x =>
                (x.LastScannedAt
                 ?? _dbContext.LinenMovementEvents
                    .Where(e => e.CustomerId == tenantCustomerId && e.LinenItemId == x.Id)
                    .Max(e => (DateTime?)e.OccurredAt)) == null
                || (x.LastScannedAt
                    ?? _dbContext.LinenMovementEvents
                        .Where(e => e.CustomerId == tenantCustomerId && e.LinenItemId == x.Id)
                        .Max(e => (DateTime?)e.OccurredAt)) < cutoff);
        }

        if (exceptionOnly)
        {
            var exceptionCutoff = DateTime.UtcNow.AddDays(-(staleDays is > 0 ? staleDays.Value : 7));
            query = query.Where(x =>
                x.CurrentLocationId == null
                || (x.LastScannedAt
                    ?? _dbContext.LinenMovementEvents
                        .Where(e => e.CustomerId == tenantCustomerId && e.LinenItemId == x.Id)
                        .Max(e => (DateTime?)e.OccurredAt)) == null
                || (x.LastScannedAt
                    ?? _dbContext.LinenMovementEvents
                        .Where(e => e.CustomerId == tenantCustomerId && e.LinenItemId == x.Id)
                        .Max(e => (DateTime?)e.OccurredAt)) < exceptionCutoff
                || _dbContext.LinenMovementEvents.Any(e =>
                    e.CustomerId == tenantCustomerId && e.LinenItemId == x.Id && e.ProcessingResult.ToLower() != "accepted"));
        }

        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "rfid" : sortBy.Trim().ToLowerInvariant();
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = sortBy switch
        {
            "status" => desc
                ? query.OrderByDescending(x => x.CurrentProcessStatus).ThenBy(x => x.RfidTag)
                : query.OrderBy(x => x.CurrentProcessStatus).ThenBy(x => x.RfidTag),
            "location" => desc
                ? query.OrderByDescending(x => x.CurrentLocation != null ? x.CurrentLocation.LocationName : null).ThenBy(x => x.RfidTag)
                : query.OrderBy(x => x.CurrentLocation != null ? x.CurrentLocation.LocationName : null).ThenBy(x => x.RfidTag),
            "owner" => desc
                ? query.OrderByDescending(x => x.OwnerCustomer != null ? x.OwnerCustomer.CustomerName : null).ThenBy(x => x.RfidTag)
                : query.OrderBy(x => x.OwnerCustomer != null ? x.OwnerCustomer.CustomerName : null).ThenBy(x => x.RfidTag),
            "updated" => desc
                ? query.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.RfidTag)
                : query.OrderBy(x => x.UpdatedAt).ThenBy(x => x.RfidTag),
            "lastscan" => desc
                ? query.OrderByDescending(x =>
                        x.LastScannedAt
                        ?? _dbContext.LinenMovementEvents
                            .Where(e => e.CustomerId == tenantCustomerId && e.LinenItemId == x.Id)
                            .Max(e => (DateTime?)e.OccurredAt))
                    .ThenBy(x => x.RfidTag)
                : query.OrderBy(x =>
                        x.LastScannedAt
                        ?? _dbContext.LinenMovementEvents
                            .Where(e => e.CustomerId == tenantCustomerId && e.LinenItemId == x.Id)
                            .Max(e => (DateTime?)e.OccurredAt))
                    .ThenBy(x => x.RfidTag),
            "condition" => desc
                ? query.OrderByDescending(x => x.PhysicalCondition).ThenBy(x => x.RfidTag)
                : query.OrderBy(x => x.PhysicalCondition).ThenBy(x => x.RfidTag),
            _ => desc
                ? query.OrderByDescending(x => x.RfidTag)
                : query.OrderBy(x => x.RfidTag)
        };

        var total = await query.CountAsync(cancellationToken);

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.RfidTag,
                x.ItemType,
                x.SizeLabel,
                x.DefaultAssignmentType,
                x.CurrentProcessStatus,
                x.PhysicalCondition,
                x.LastScannedAt,
                x.LifecycleState,
                x.IsActive,
                OwnerCustomerName = x.OwnerCustomer != null ? x.OwnerCustomer.CustomerName : null,
                AssignedEmployeeName = x.AssignedEmployee != null ? x.AssignedEmployee.EmployeeName : null,
                CurrentLocationName = x.CurrentLocation != null ? x.CurrentLocation.LocationName : null
            })
            .ToListAsync(cancellationToken);
        var pageIds = rows.Select(x => x.Id).ToList();

        var rejectedByItem = pageIds.Count == 0
            ? new Dictionary<ulong, int>()
            : await _dbContext.LinenMovementEvents.AsNoTracking()
                .Where(e => e.CustomerId == tenantCustomerId && pageIds.Contains(e.LinenItemId) && e.ProcessingResult.ToLower() != "accepted")
                .GroupBy(e => e.LinenItemId)
                .Select(g => new { Id = g.Key, C = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);
        var lastScanByItem = pageIds.Count == 0
            ? new Dictionary<ulong, DateTime?>()
            : await _dbContext.LinenMovementEvents.AsNoTracking()
                .Where(e => e.CustomerId == tenantCustomerId && pageIds.Contains(e.LinenItemId))
                .GroupBy(e => e.LinenItemId)
                .Select(g => new { Id = g.Key, Last = (DateTime?)g.Max(x => x.OccurredAt) })
                .ToDictionaryAsync(x => x.Id, x => x.Last, cancellationToken);

        var now = DateTime.UtcNow;
        var items = rows.Select(x => new LinenItemHubRowViewModel
        {
            Id = x.Id,
            RfidTag = x.RfidTag,
            ItemType = x.ItemType,
            SizeLabel = x.SizeLabel,
            DefaultAssignmentType = x.DefaultAssignmentType,
            CurrentProcessStatus = x.CurrentProcessStatus,
            PhysicalCondition = x.PhysicalCondition,
            LastScannedAt = x.LastScannedAt ?? lastScanByItem.GetValueOrDefault(x.Id),
            DaysSinceLastScan = (x.LastScannedAt ?? lastScanByItem.GetValueOrDefault(x.Id)) is { } seenAt
                ? (int)(now - seenAt).TotalDays
                : null,
            RecentRejectedCount = rejectedByItem.GetValueOrDefault(x.Id),
            LifecycleState = x.LifecycleState,
            IsActive = x.IsActive,
            OwnerCustomerName = x.OwnerCustomerName,
            AssignedEmployeeName = x.AssignedEmployeeName,
            CurrentLocationName = x.CurrentLocationName
        }).ToList();

        var customers = await _dbContext.Customers
            .AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == tenantCustomerId)
            .OrderBy(x => x.CustomerName)
            .Select(x => new CustomerOptionViewModel { Id = x.Id, CustomerName = x.CustomerName })
            .ToListAsync(cancellationToken);

        var locations = await _dbContext.Locations
            .AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == tenantCustomerId)
            .OrderBy(x => x.LocationName)
            .Select(x => new LocationOptionViewModel { Id = x.Id, LocationName = x.LocationName })
            .ToListAsync(cancellationToken);

        var processStatuses = await _dbContext.LinenItems
            .AsNoTracking()
            .Where(x => x.CustomerId == tenantCustomerId)
            .Select(x => x.CurrentProcessStatus)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        var physicalConditions = await _dbContext.LinenItems
            .AsNoTracking()
            .Where(x => x.CustomerId == tenantCustomerId)
            .Select(x => x.PhysicalCondition)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
        var processingResults = await _dbContext.LinenMovementEvents
            .AsNoTracking()
            .Where(x => x.CustomerId == tenantCustomerId)
            .Select(x => x.ProcessingResult)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        return View(new LinenItemsIndexViewModel
        {
            Items = items,
            Query = new LinenItemsQueryViewModel
            {
                Q = q,
                CustomerId = customerId,
                LocationId = locationUnassigned ? null : locationId,
                LocationUnassigned = locationUnassigned,
                ProcessStatus = string.IsNullOrWhiteSpace(processStatus) ? null : processStatus.Trim(),
                PhysicalCondition = string.IsNullOrWhiteSpace(physicalCondition) ? null : physicalCondition.Trim(),
                ProcessingResult = string.IsNullOrWhiteSpace(processingResult) ? null : processingResult.Trim(),
                StaleDays = staleDays,
                ExceptionOnly = exceptionOnly,
                SortBy = sortBy,
                SortDir = desc ? "desc" : "asc",
                IncludeInactive = includeInactive
            },
            Customers = customers,
            Locations = locations,
            ProcessStatuses = processStatuses,
            PhysicalConditions = physicalConditions,
            ProcessingResults = processingResults,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<IActionResult> Details(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var row = await _dbContext.LinenItems
            .AsNoTracking()
            .Where(x => x.Id == id && x.CustomerId == customerId)
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
                x.LifecycleState,
                x.DeactivationReason,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row == null)
            return NotFound();

        var timeline = await _dbContext.LinenMovementEvents
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.LinenItemId == id)
            .OrderByDescending(x => x.OccurredAt)
            .Take(TimelineTake)
            .Select(x => new LinenItemTimelineEventViewModel
            {
                EventId = x.Id,
                OccurredAt = x.OccurredAt,
                ReaderName = x.Reader.ReaderName,
                ScanRouteName = x.ReaderWay.WayName,
                ProcessingResult = x.ProcessingResult,
                ConditionAfterEvent = x.ConditionAfterEvent,
                RejectionReason = x.RejectionReason,
                LogisticsJobId = x.LogisticsJobId,
                LogisticsJobType = x.LogisticsJob != null ? x.LogisticsJob.JobType : null
            })
            .ToListAsync(cancellationToken);

        var totalScanCount = await _dbContext.LinenMovementEvents.AsNoTracking()
            .CountAsync(x => x.CustomerId == customerId && x.LinenItemId == id, cancellationToken);
        var rejectedScanCount = await _dbContext.LinenMovementEvents.AsNoTracking()
            .CountAsync(x => x.CustomerId == customerId && x.LinenItemId == id && x.ProcessingResult.ToLower() != "accepted", cancellationToken);
        var lastAcceptedScanAt = await _dbContext.LinenMovementEvents.AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.LinenItemId == id && x.ProcessingResult.ToLower() == "accepted")
            .MaxAsync(x => (DateTime?)x.OccurredAt, cancellationToken);
        var now = DateTime.UtcNow;

        var model = new LinenItemDetailViewModel
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
            LastScannedAt = row.LastScannedAt ?? timeline.FirstOrDefault()?.OccurredAt,
            LifecycleState = row.LifecycleState,
            DeactivationReason = row.DeactivationReason,
            IsActive = row.IsActive,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt,
            TotalScanCount = totalScanCount,
            RejectedScanCount = rejectedScanCount,
            LastAcceptedScanAt = lastAcceptedScanAt,
            DaysSinceLastScan = (row.LastScannedAt ?? timeline.FirstOrDefault()?.OccurredAt) is { } seenAt
                ? (int)(now - seenAt).TotalDays
                : null,
            Timeline = timeline
        };

        return View(model);
    }

    private static string NormalizeStatus(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "(empty)"
            : value.Trim().ToLowerInvariant();
    }

}
