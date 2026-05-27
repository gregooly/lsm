using LaundryMS.Web.Data;
using LaundryMS.Web.Models.Entities;
using LaundryMS.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Controllers;

public class LocationsController : TenantScopedController
{
    private readonly LaundryMsDbContext _dbContext;

    public LocationsController(LaundryMsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(
        string? q,
        string? locationType,
        ulong? customerId,
        bool includeInactive = false,
        string sortBy = "name",
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

        var baseQuery = _dbContext.Locations.AsNoTracking();
        if (!includeInactive)
            baseQuery = baseQuery.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            baseQuery = baseQuery.Where(x =>
                x.LocationName.Contains(term)
                || x.LocationType.Contains(term)
                || (x.LocationAddressText != null && x.LocationAddressText.Contains(term))
                || (x.ContactPerson != null && x.ContactPerson.Contains(term))
                || (x.ContactPhone != null && x.ContactPhone.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(locationType))
        {
            var t = locationType.Trim().ToLowerInvariant();
            baseQuery = baseQuery.Where(x => x.LocationType == t);
        }

        if (customerId.HasValue)
            baseQuery = baseQuery.Where(x => x.CustomerId == customerId.Value);

        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "name" : sortBy.Trim().ToLowerInvariant();
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        baseQuery = sortBy switch
        {
            "type" => desc
                ? baseQuery.OrderByDescending(x => x.LocationType).ThenBy(x => x.LocationName)
                : baseQuery.OrderBy(x => x.LocationType).ThenBy(x => x.LocationName),
            "customer" => desc
                ? baseQuery.OrderByDescending(x => x.LocationName)
                : baseQuery.OrderBy(x => x.LocationName),
            "status" => desc
                ? baseQuery.OrderByDescending(x => x.IsActive).ThenBy(x => x.LocationName)
                : baseQuery.OrderBy(x => x.IsActive).ThenBy(x => x.LocationName),
            _ => desc
                ? baseQuery.OrderByDescending(x => x.LocationName)
                : baseQuery.OrderBy(x => x.LocationName)
        };

        var total = await baseQuery.CountAsync(cancellationToken);
        var pageRows = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.LocationName,
                x.LocationType,
                x.LocationAddressText,
                CustomerName = (string?)null,
                x.IsActive
            })
            .ToListAsync(cancellationToken);

        var ids = pageRows.Select(x => x.Id).ToList();
        var metrics = await LoadLocationMetricsAsync(ids, tenantCustomerId, cancellationToken);

        var model = new LocationsIndexViewModel
        {
            Items = pageRows.Select(x => new LocationListItemViewModel
            {
                Id = x.Id,
                LocationName = x.LocationName,
                LocationType = x.LocationType,
                CustomerName = x.CustomerName,
                LocationAddressText = x.LocationAddressText,
                LinkedLinenCount = metrics.Linen.GetValueOrDefault(x.Id),
                LinkedReaderWaysCount = metrics.ReaderWays.GetValueOrDefault(x.Id),
                OpenLogisticsJobsCount = metrics.OpenJobs.GetValueOrDefault(x.Id),
                IsActive = x.IsActive
            }).ToList(),
            CustomerOptions = await _dbContext.Customers.AsNoTracking()
                .Where(x => x.IsActive && x.CustomerId == tenantCustomerId)
                .OrderBy(x => x.CustomerName)
                .Select(x => new CustomerOptionViewModel { Id = x.Id, CustomerName = x.CustomerName })
                .ToListAsync(cancellationToken),
            LocationTypeOptions = await _dbContext.Locations.AsNoTracking()
                .Where(x => x.CustomerId == tenantCustomerId)
                .Select(x => x.LocationType)
                .Where(x => x != "")
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(cancellationToken),
            Query = new LocationsQueryViewModel
            {
                Q = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
                LocationType = string.IsNullOrWhiteSpace(locationType) ? null : locationType.Trim().ToLowerInvariant(),
                CustomerId = customerId,
                IncludeInactive = includeInactive,
                SortBy = sortBy,
                SortDir = desc ? "desc" : "asc"
            },
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };

        return View(model);
    }

    public async Task<IActionResult> Details(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var loc = await _dbContext.Locations.AsNoTracking()
            .Where(x => x.Id == id && x.CustomerId == customerId)
            .Select(x => new
            {
                x.Id,
                x.LocationName,
                x.LocationType,
                x.CustomerId,
                CustomerName = (string?)null,
                x.LocationAddressText,
                x.ContactPerson,
                x.ContactPhone,
                x.GeoLat,
                x.GeoLng,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (loc is null)
            return NotFound();

        var metrics = await LoadLocationMetricsAsync([id], customerId, cancellationToken);

        var model = new LocationDetailViewModel
        {
            Id = loc.Id,
            LocationName = loc.LocationName,
            LocationType = loc.LocationType,
            CustomerId = loc.CustomerId,
            CustomerName = loc.CustomerName,
            LocationAddressText = loc.LocationAddressText,
            ContactPerson = loc.ContactPerson,
            ContactPhone = loc.ContactPhone,
            GeoLat = loc.GeoLat,
            GeoLng = loc.GeoLng,
            IsActive = loc.IsActive,
            CreatedAt = loc.CreatedAt,
            UpdatedAt = loc.UpdatedAt,
            LinkedLinenCount = metrics.Linen.GetValueOrDefault(id),
            LinkedReaderWaysCount = metrics.ReaderWays.GetValueOrDefault(id),
            OpenLogisticsJobsCount = metrics.OpenJobs.GetValueOrDefault(id)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Get(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var loc = await _dbContext.Locations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);

        if (loc is null)
            return NotFound();

        return Json(new
        {
            loc.Id,
            loc.LocationName,
            loc.LocationType,
            loc.CustomerId,
            loc.LocationAddressText,
            loc.ContactPerson,
            loc.ContactPhone,
            loc.GeoLat,
            loc.GeoLng,
            loc.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] LocationFormViewModel model, CancellationToken cancellationToken)
    {
        NormalizeForm(model);
        if (!TryGetCurrentCustomerId(out var tenantCustomerId))
            return Forbid();
        model.CustomerId = tenantCustomerId;
        TryValidateModel(model);
        if (!ModelState.IsValid)
            return ModalValidationResult();
        if (!await ValidateCustomerIdAsync(model.CustomerId, cancellationToken))
            return ModalValidationResult();

        var now = DateTime.UtcNow;
        _dbContext.Locations.Add(new Location
        {
            LocationName = model.LocationName.Trim(),
            LocationType = model.LocationType.Trim(),
            CustomerId = model.CustomerId,
            LocationAddressText = model.LocationAddressText,
            ContactPerson = model.ContactPerson,
            ContactPhone = model.ContactPhone,
            GeoLat = model.GeoLat,
            GeoLng = model.GeoLng,
            IsActive = model.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromForm] LocationFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.Id is null or 0)
        {
            ModelState.AddModelError(nameof(model.Id), "Missing location id.");
            return ModalValidationResult();
        }

        NormalizeForm(model);
        if (!TryGetCurrentCustomerId(out var tenantCustomerId))
            return Forbid();
        model.CustomerId = tenantCustomerId;
        TryValidateModel(model);
        if (!ModelState.IsValid)
            return ModalValidationResult();
        if (!await ValidateCustomerIdAsync(model.CustomerId, cancellationToken))
            return ModalValidationResult();

        var entity = await _dbContext.Locations.FirstOrDefaultAsync(x => x.Id == model.Id && x.CustomerId == tenantCustomerId, cancellationToken);
        if (entity is null)
            return NotFound();

        entity.LocationName = model.LocationName.Trim();
        entity.LocationType = model.LocationType.Trim();
        entity.CustomerId = model.CustomerId;
        entity.LocationAddressText = model.LocationAddressText;
        entity.ContactPerson = model.ContactPerson;
        entity.ContactPhone = model.ContactPhone;
        entity.GeoLat = model.GeoLat;
        entity.GeoLng = model.GeoLng;
        entity.IsActive = model.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var entity = await _dbContext.Locations.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);
        if (entity is null)
            return NotFound();

        var linenLinkedCount = await _dbContext.LinenItems.AsNoTracking()
            .CountAsync(x => x.CustomerId == customerId && x.CurrentLocationId == id, cancellationToken);

        var readerWaysLinkedCount = await _dbContext.ReaderWays.AsNoTracking()
            .CountAsync(x => x.CustomerId == customerId && (x.FromLocationId == id || x.ToLocationId == id), cancellationToken);
        var jobsLinkedCount = await _dbContext.LogisticsJobs.AsNoTracking()
            .CountAsync(x => x.CustomerId == customerId && (x.FromLocationId == id || x.ToLocationId == id), cancellationToken);

        if (linenLinkedCount > 0 || readerWaysLinkedCount > 0 || jobsLinkedCount > 0)
        {
            return BadRequest(new
            {
                ok = false,
                message = "Cannot delete this location while it is linked to linen items, scan routes, or logistics jobs. Change those links or edit the record."
            });
        }

        _dbContext.Locations.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Json(new { ok = true });
    }

    private IActionResult ModalValidationResult()
    {
        var errors = ModelState
            .Where(x => x.Value is { Errors.Count: > 0 })
            .ToDictionary(
                x => x.Key,
                x => x.Value!.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage).ToArray());

        return BadRequest(new { ok = false, errors });
    }

    private static void NormalizeForm(LocationFormViewModel model)
    {
        model.LocationName = string.IsNullOrWhiteSpace(model.LocationName) ? string.Empty : model.LocationName.Trim();
        model.LocationType = string.IsNullOrWhiteSpace(model.LocationType) ? string.Empty : model.LocationType.Trim().ToLowerInvariant();
        if (model.CustomerId is 0)
            model.CustomerId = null;
        model.LocationAddressText = string.IsNullOrWhiteSpace(model.LocationAddressText) ? null : model.LocationAddressText.Trim();
        model.ContactPerson = string.IsNullOrWhiteSpace(model.ContactPerson) ? null : model.ContactPerson.Trim();
        model.ContactPhone = string.IsNullOrWhiteSpace(model.ContactPhone) ? null : model.ContactPhone.Trim();
    }

    private async Task<bool> ValidateCustomerIdAsync(ulong? customerId, CancellationToken cancellationToken)
    {
        if (!customerId.HasValue)
            return true;

        var exists = await _dbContext.Customers.AsNoTracking()
            .AnyAsync(x => x.CustomerId == customerId.Value, cancellationToken);
        if (exists)
            return true;

        ModelState.AddModelError(nameof(LocationFormViewModel.CustomerId), "Selected customer was not found.");
        return false;
    }


    private sealed class LocationMetricsBundle
    {
        public Dictionary<ulong, int> Linen { get; init; } = new();
        public Dictionary<ulong, int> ReaderWays { get; init; } = new();
        public Dictionary<ulong, int> OpenJobs { get; init; } = new();
    }

    private async Task<LocationMetricsBundle> LoadLocationMetricsAsync(IReadOnlyList<ulong> locationIds, ulong customerId, CancellationToken cancellationToken)
    {
        if (locationIds.Count == 0)
            return new LocationMetricsBundle();

        var linen = await _dbContext.LinenItems.AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.CurrentLocationId != null && locationIds.Contains(x.CurrentLocationId.Value))
            .GroupBy(x => x.CurrentLocationId!.Value)
            .Select(g => new { Id = g.Key, C = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);

        var readerWaysFrom = await _dbContext.ReaderWays.AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.FromLocationId != null && locationIds.Contains(x.FromLocationId.Value))
            .GroupBy(x => x.FromLocationId!.Value)
            .Select(g => new { Id = g.Key, C = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);
        var readerWaysTo = await _dbContext.ReaderWays.AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.ToLocationId != null && locationIds.Contains(x.ToLocationId.Value))
            .GroupBy(x => x.ToLocationId!.Value)
            .Select(g => new { Id = g.Key, C = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);
        var readerWays = locationIds.ToDictionary(
            id => id,
            id => readerWaysFrom.GetValueOrDefault(id) + readerWaysTo.GetValueOrDefault(id));

        var openJobsFrom = await _dbContext.LogisticsJobs.AsNoTracking()
            .Where(x =>
                x.CustomerId == customerId
                &&
                x.FromLocationId != null
                && locationIds.Contains(x.FromLocationId.Value)
                && (x.JobStatus == "open" || x.JobStatus == "in_progress"))
            .GroupBy(x => x.FromLocationId!.Value)
            .Select(g => new { Id = g.Key, C = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);
        var openJobsTo = await _dbContext.LogisticsJobs.AsNoTracking()
            .Where(x =>
                x.CustomerId == customerId
                &&
                x.ToLocationId != null
                && locationIds.Contains(x.ToLocationId.Value)
                && (x.JobStatus == "open" || x.JobStatus == "in_progress"))
            .GroupBy(x => x.ToLocationId!.Value)
            .Select(g => new { Id = g.Key, C = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);
        var openJobs = locationIds.ToDictionary(
            id => id,
            id => openJobsFrom.GetValueOrDefault(id) + openJobsTo.GetValueOrDefault(id));

        return new LocationMetricsBundle
        {
            Linen = linen,
            ReaderWays = readerWays,
            OpenJobs = openJobs
        };
    }
}
