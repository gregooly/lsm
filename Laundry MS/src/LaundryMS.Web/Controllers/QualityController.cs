using LaundryMS.Web.Data;
using LaundryMS.Web.Models.Entities;
using LaundryMS.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Controllers;

public class QualityController : TenantScopedController
{
    private readonly LaundryMsDbContext _dbContext;

    public QualityController(LaundryMsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(
        string? q,
        ulong? customerId,
        ulong? locationId,
        string? condition,
        string? processStatus,
        int? staleDays,
        bool includeInactive = false,
        string sortBy = "rfid",
        string sortDir = "asc",
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
        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(condition))
        {
            var cond = condition.Trim().ToLowerInvariant();
            query = query.Where(x => x.PhysicalCondition == cond);
        }
        else
        {
            query = query.Where(x => x.PhysicalCondition != "good");
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.RfidTag.Contains(term)
                || x.ItemType.Contains(term)
                || (x.OwnerCustomer != null && x.OwnerCustomer.CustomerName.Contains(term))
                || (x.CurrentLocation != null && x.CurrentLocation.LocationName.Contains(term)));
        }

        if (customerId.HasValue)
            query = query.Where(x => x.CustomerId == customerId.Value);

        if (locationId.HasValue)
            query = query.Where(x => x.CurrentLocationId == locationId.Value);

        if (!string.IsNullOrWhiteSpace(processStatus))
        {
            var s = processStatus.Trim().ToLowerInvariant();
            query = query.Where(x => x.CurrentProcessStatus == s);
        }

        if (staleDays is > 0)
        {
            var cutoff = DateTime.UtcNow.AddDays(-staleDays.Value);
            query = query.Where(x => x.LastScannedAt == null || x.LastScannedAt < cutoff);
        }

        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "rfid" : sortBy.Trim().ToLowerInvariant();
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = sortBy switch
        {
            "condition" => desc
                ? query.OrderByDescending(x => x.PhysicalCondition).ThenBy(x => x.RfidTag)
                : query.OrderBy(x => x.PhysicalCondition).ThenBy(x => x.RfidTag),
            "status" => desc
                ? query.OrderByDescending(x => x.CurrentProcessStatus).ThenBy(x => x.RfidTag)
                : query.OrderBy(x => x.CurrentProcessStatus).ThenBy(x => x.RfidTag),
            "owner" => desc
                ? query.OrderByDescending(x => x.OwnerCustomer != null ? x.OwnerCustomer.CustomerName : null).ThenBy(x => x.RfidTag)
                : query.OrderBy(x => x.OwnerCustomer != null ? x.OwnerCustomer.CustomerName : null).ThenBy(x => x.RfidTag),
            "location" => desc
                ? query.OrderByDescending(x => x.CurrentLocation != null ? x.CurrentLocation.LocationName : null).ThenBy(x => x.RfidTag)
                : query.OrderBy(x => x.CurrentLocation != null ? x.CurrentLocation.LocationName : null).ThenBy(x => x.RfidTag),
            "lastscan" => desc
                ? query.OrderByDescending(x => x.LastScannedAt).ThenBy(x => x.RfidTag)
                : query.OrderBy(x => x.LastScannedAt).ThenBy(x => x.RfidTag),
            "updated" => desc
                ? query.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.RfidTag)
                : query.OrderBy(x => x.UpdatedAt).ThenBy(x => x.RfidTag),
            _ => desc
                ? query.OrderByDescending(x => x.RfidTag)
                : query.OrderBy(x => x.RfidTag)
        };

        var total = await query.CountAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.RfidTag,
                x.ItemType,
                x.PhysicalCondition,
                x.CurrentProcessStatus,
                OwnerCustomerName = x.OwnerCustomer != null ? x.OwnerCustomer.CustomerName : null,
                CurrentLocationName = x.CurrentLocation != null ? x.CurrentLocation.LocationName : null,
                x.LastScannedAt,
                x.UpdatedAt,
                x.LifecycleState
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(x => new QualityItemRowViewModel
        {
            Id = x.Id,
            RfidTag = x.RfidTag,
            ItemType = x.ItemType,
            PhysicalCondition = x.PhysicalCondition,
            CurrentProcessStatus = x.CurrentProcessStatus,
            OwnerCustomerName = x.OwnerCustomerName,
            CurrentLocationName = x.CurrentLocationName,
            LastScannedAt = x.LastScannedAt,
            DaysSinceLastScan = x.LastScannedAt.HasValue ? (int)(now - x.LastScannedAt.Value).TotalDays : null,
            UpdatedAt = x.UpdatedAt,
            LifecycleState = x.LifecycleState
        }).ToList();

        var customers = await _dbContext.Customers.AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == tenantCustomerId)
            .OrderBy(x => x.CustomerName)
            .Select(x => new CustomerOptionViewModel { Id = x.Id, CustomerName = x.CustomerName })
            .ToListAsync(cancellationToken);
        var locations = await _dbContext.Locations.AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == tenantCustomerId)
            .OrderBy(x => x.LocationName)
            .Select(x => new LocationOptionViewModel { Id = x.Id, LocationName = x.LocationName })
            .ToListAsync(cancellationToken);
        var statuses = await _dbContext.LinenItems.AsNoTracking()
            .Where(x => x.CustomerId == tenantCustomerId)
            .Select(x => x.CurrentProcessStatus).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
        var conditions = await _dbContext.LinenItems.AsNoTracking()
            .Where(x => x.CustomerId == tenantCustomerId)
            .Select(x => x.PhysicalCondition).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);

        var openIssues = await _dbContext.LinenItems.AsNoTracking()
            .CountAsync(x => x.CustomerId == tenantCustomerId && x.IsActive && x.PhysicalCondition != "good", cancellationToken);
        var damaged = await _dbContext.LinenItems.AsNoTracking()
            .CountAsync(x => x.CustomerId == tenantCustomerId && x.IsActive && x.PhysicalCondition == "damaged", cancellationToken);
        var lost = await _dbContext.LinenItems.AsNoTracking()
            .CountAsync(x => x.CustomerId == tenantCustomerId && x.IsActive && x.PhysicalCondition == "lost", cancellationToken);

        return View(new QualityIndexViewModel
        {
            Items = items,
            Customers = customers,
            Locations = locations,
            ProcessStatuses = statuses,
            Conditions = conditions,
            Query = new QualityQueryViewModel
            {
                Q = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
                CustomerId = customerId,
                LocationId = locationId,
                Condition = string.IsNullOrWhiteSpace(condition) ? null : condition.Trim().ToLowerInvariant(),
                ProcessStatus = string.IsNullOrWhiteSpace(processStatus) ? null : processStatus.Trim().ToLowerInvariant(),
                StaleDays = staleDays,
                IncludeInactive = includeInactive,
                SortBy = sortBy,
                SortDir = desc ? "desc" : "asc"
            },
            OpenIssuesCount = openIssues,
            DamagedCount = damaged,
            LostCount = lost,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCondition([FromForm] QualityActionViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var item = await _dbContext.LinenItems.FirstOrDefaultAsync(x => x.Id == model.LinenItemId && x.CustomerId == customerId, cancellationToken);
        if (item is null)
            return Json(new { ok = false, message = "Item not found." });

        var toCondition = string.IsNullOrWhiteSpace(model.NewCondition)
            ? null
            : model.NewCondition.Trim().ToLowerInvariant();
        if (toCondition is not ("good" or "damaged" or "lost"))
            return Json(new { ok = false, message = "Invalid condition value." });

        var fromCondition = item.PhysicalCondition;
        item.PhysicalCondition = toCondition;
        item.UpdatedAt = DateTime.UtcNow;
        if (toCondition == "good" && item.LifecycleState == "discarded")
            item.LifecycleState = "active";

        _dbContext.LinenQualityEvents.Add(new LinenQualityEvent
        {
            CustomerId = customerId,
            LinenItemId = item.Id,
            EventType = model.EventType ?? (toCondition == "good" ? "repaired" : toCondition == "lost" ? "lost_confirmed" : "reported"),
            FromCondition = fromCondition,
            ToCondition = toCondition,
            Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim(),
            ReportedBy = User.Identity?.Name,
            ResolvedBy = toCondition == "good" ? User.Identity?.Name : null,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNote([FromForm] QualityActionViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        if (model.LinenItemId == 0)
            return Json(new { ok = false, message = "Missing item id." });
        if (string.IsNullOrWhiteSpace(model.Note))
            return Json(new { ok = false, message = "Note is required." });

        var exists = await _dbContext.LinenItems.AsNoTracking()
            .AnyAsync(x => x.Id == model.LinenItemId && x.CustomerId == customerId, cancellationToken);
        if (!exists)
            return Json(new { ok = false, message = "Item not found." });

        _dbContext.LinenQualityEvents.Add(new LinenQualityEvent
        {
            CustomerId = customerId,
            LinenItemId = model.LinenItemId,
            EventType = "note",
            Note = model.Note.Trim(),
            ReportedBy = User.Identity?.Name,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Json(new { ok = true });
    }

}
