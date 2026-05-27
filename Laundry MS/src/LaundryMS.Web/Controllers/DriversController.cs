using LaundryMS.Web.Data;
using LaundryMS.Web.Auth;
using LaundryMS.Web.Models.Entities;
using LaundryMS.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Controllers;

public class DriversController : TenantScopedController
{
    private readonly LaundryMsDbContext _dbContext;

    public DriversController(LaundryMsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(
        string? q,
        bool includeInactive = false,
        string sortBy = "name",
        string sortDir = "asc",
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 10 or > 200 ? 50 : pageSize;

        var baseQuery = _dbContext.Drivers.AsNoTracking().Where(x => x.CustomerId == customerId);
        if (!includeInactive)
            baseQuery = baseQuery.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            baseQuery = baseQuery.Where(x =>
                x.DriverName.Contains(term)
                || (x.MobilePhone != null && x.MobilePhone.Contains(term))
                || (x.VehicleRegistrationNo != null && x.VehicleRegistrationNo.Contains(term))
                || (x.HandheldDeviceId != null && x.HandheldDeviceId.Contains(term)));
        }

        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "name" : sortBy.Trim().ToLowerInvariant();
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        baseQuery = sortBy switch
        {
            "status" => desc
                ? baseQuery.OrderByDescending(x => x.IsActive).ThenBy(x => x.DriverName)
                : baseQuery.OrderBy(x => x.IsActive).ThenBy(x => x.DriverName),
            "vehicle" => desc
                ? baseQuery.OrderByDescending(x => x.VehicleRegistrationNo).ThenBy(x => x.DriverName)
                : baseQuery.OrderBy(x => x.VehicleRegistrationNo).ThenBy(x => x.DriverName),
            _ => desc
                ? baseQuery.OrderByDescending(x => x.DriverName)
                : baseQuery.OrderBy(x => x.DriverName)
        };

        var total = await baseQuery.CountAsync(cancellationToken);

        var pageRows = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.DriverName,
                x.MobilePhone,
                x.VehicleRegistrationNo,
                x.HandheldDeviceId,
                x.IsActive
            })
            .ToListAsync(cancellationToken);

        var pageIds = pageRows.Select(x => x.Id).ToList();
        var since = DateTime.UtcNow.AddDays(-90);
        var metrics = await LoadDriverMetricsAsync(pageIds, since, customerId, cancellationToken);

        var items = pageRows.Select(x => new DriverListItemViewModel
        {
            Id = x.Id,
            DriverName = x.DriverName,
            MobilePhone = x.MobilePhone,
            VehicleRegistrationNo = x.VehicleRegistrationNo,
            HandheldDeviceId = x.HandheldDeviceId,
            IsActive = x.IsActive,
            RecentCollectionJobCount = metrics.Collections90d.GetValueOrDefault(x.Id),
            RecentDeliveryJobCount = metrics.Deliveries90d.GetValueOrDefault(x.Id),
            OpenLogisticsJobsCount = metrics.OpenJobs.GetValueOrDefault(x.Id)
        }).ToList();

        return View(new DriversIndexViewModel
        {
            Items = items,
            Query = new DriversQueryViewModel
            {
                Q = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
                IncludeInactive = includeInactive,
                SortBy = sortBy,
                SortDir = desc ? "desc" : "asc"
            },
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<IActionResult> Details(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var d = await _dbContext.Drivers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);

        if (d is null)
            return NotFound();

        var since = DateTime.UtcNow.AddDays(-90);
        var metrics = await LoadDriverMetricsAsync([id], since, customerId, cancellationToken);
        var scanCount = await _dbContext.LinenMovementEvents.AsNoTracking()
            .CountAsync(x => x.CustomerId == customerId && x.DriverId == id, cancellationToken);

        var model = new DriverDetailViewModel
        {
            Id = d.Id,
            DriverName = d.DriverName,
            MobilePhone = d.MobilePhone,
            VehicleRegistrationNo = d.VehicleRegistrationNo,
            HandheldDeviceId = d.HandheldDeviceId,
            IsActive = d.IsActive,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt,
            RecentCollectionJobCount = metrics.Collections90d.GetValueOrDefault(id),
            RecentDeliveryJobCount = metrics.Deliveries90d.GetValueOrDefault(id),
            OpenLogisticsJobsCount = metrics.OpenJobs.GetValueOrDefault(id),
            LinkedScanEventCount = scanCount
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Get(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var d = await _dbContext.Drivers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);

        if (d is null)
            return NotFound();

        return Json(new
        {
            d.Id,
            d.DriverName,
            d.MobilePhone,
            d.VehicleRegistrationNo,
            d.HandheldDeviceId,
            d.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] DriverFormViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        NormalizeForm(model);
        TryValidateModel(model);
        if (!ModelState.IsValid)
            return ModalValidationResult();

        var dupErr = await ValidateHandheldDeviceUniqueAsync(model.HandheldDeviceId, excludeDriverId: null, customerId, cancellationToken);
        if (dupErr != null)
            return BadRequest(new { ok = false, errors = new Dictionary<string, string[]> { [nameof(DriverFormViewModel.HandheldDeviceId)] = [dupErr] } });

        var now = DateTime.UtcNow;
        _dbContext.Drivers.Add(new Driver
        {
            CustomerId = customerId,
            DriverName = model.DriverName.Trim(),
            MobilePhone = model.MobilePhone,
            VehicleRegistrationNo = model.VehicleRegistrationNo,
            HandheldDeviceId = model.HandheldDeviceId,
            IsActive = model.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromForm] DriverFormViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        if (model.Id is null or 0)
        {
            ModelState.AddModelError(nameof(model.Id), "Missing driver id.");
            return ModalValidationResult();
        }

        NormalizeForm(model);
        TryValidateModel(model);
        if (!ModelState.IsValid)
            return ModalValidationResult();

        var dupErr = await ValidateHandheldDeviceUniqueAsync(model.HandheldDeviceId, model.Id, customerId, cancellationToken);
        if (dupErr != null)
            return BadRequest(new { ok = false, errors = new Dictionary<string, string[]> { [nameof(DriverFormViewModel.HandheldDeviceId)] = [dupErr] } });

        var entity = await _dbContext.Drivers.FirstOrDefaultAsync(x => x.Id == model.Id && x.CustomerId == customerId, cancellationToken);
        if (entity is null)
            return NotFound();

        entity.DriverName = model.DriverName.Trim();
        entity.MobilePhone = model.MobilePhone;
        entity.VehicleRegistrationNo = model.VehicleRegistrationNo;
        entity.HandheldDeviceId = model.HandheldDeviceId;
        entity.IsActive = model.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var entity = await _dbContext.Drivers.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);
        if (entity is null)
            return NotFound();

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var entity = await _dbContext.Drivers.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);
        if (entity is null)
            return NotFound();

        entity.IsActive = true;
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

        var entity = await _dbContext.Drivers.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);
        if (entity is null)
            return NotFound();

        var jobCount = await _dbContext.LogisticsJobs.AsNoTracking()
            .CountAsync(x => x.CustomerId == customerId && x.DriverId == id, cancellationToken);
        var scanCount = await _dbContext.LinenMovementEvents.AsNoTracking()
            .CountAsync(x => x.CustomerId == customerId && x.DriverId == id, cancellationToken);

        if (jobCount > 0 || scanCount > 0)
        {
            return BadRequest(new
            {
                ok = false,
                message = "Cannot delete this driver while they are linked to jobs or scans. Use deactivate instead."
            });
        }

        _dbContext.Drivers.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Json(new { ok = true });
    }

    private sealed class DriverMetricsBundle
    {
        public Dictionary<ulong, int> Collections90d { get; init; } = new();
        public Dictionary<ulong, int> Deliveries90d { get; init; } = new();
        public Dictionary<ulong, int> OpenJobs { get; init; } = new();
    }

    private async Task<DriverMetricsBundle> LoadDriverMetricsAsync(
        IReadOnlyList<ulong> driverIds,
        DateTime since,
        ulong customerId,
        CancellationToken cancellationToken)
    {
        if (driverIds.Count == 0)
            return new DriverMetricsBundle();

        var collections = await _dbContext.LogisticsJobs
            .AsNoTracking()
            .Where(j => j.CustomerId == customerId && j.DriverId != null && driverIds.Contains(j.DriverId.Value))
            .WhereCollectionJobType()
            .Where(j => j.CreatedAt >= since)
            .GroupBy(j => j.DriverId!.Value)
            .Select(g => new { Id = g.Key, C = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);

        var deliveries = await _dbContext.LogisticsJobs
            .AsNoTracking()
            .Where(j => j.CustomerId == customerId && j.DriverId != null && driverIds.Contains(j.DriverId.Value))
            .WhereDeliveryJobType()
            .Where(j => j.CreatedAt >= since)
            .GroupBy(j => j.DriverId!.Value)
            .Select(g => new { Id = g.Key, C = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);

        var openJobs = await _dbContext.LogisticsJobs
            .AsNoTracking()
            .Where(j =>
                j.DriverId != null
                && j.CustomerId == customerId
                && driverIds.Contains(j.DriverId.Value)
                && (j.JobStatus == "open" || j.JobStatus == "in_progress"))
            .GroupBy(j => j.DriverId!.Value)
            .Select(g => new { Id = g.Key, C = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);

        return new DriverMetricsBundle
        {
            Collections90d = collections,
            Deliveries90d = deliveries,
            OpenJobs = openJobs
        };
    }

    private async Task<string?> ValidateHandheldDeviceUniqueAsync(
        string? handheldDeviceId,
        ulong? excludeDriverId,
        ulong customerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(handheldDeviceId))
            return null;

        var v = handheldDeviceId.Trim();
        var query = _dbContext.Drivers.AsNoTracking().Where(x => x.CustomerId == customerId && x.HandheldDeviceId == v);
        if (excludeDriverId.HasValue)
            query = query.Where(x => x.Id != excludeDriverId.Value);

        return await query.AnyAsync(cancellationToken)
            ? "This handheld device ID is already assigned to another driver."
            : null;
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

    private static void NormalizeForm(DriverFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.DriverName))
            model.DriverName = string.Empty;
        else
            model.DriverName = model.DriverName.Trim();

        model.MobilePhone = string.IsNullOrWhiteSpace(model.MobilePhone) ? null : model.MobilePhone.Trim();
        model.VehicleRegistrationNo = string.IsNullOrWhiteSpace(model.VehicleRegistrationNo) ? null : model.VehicleRegistrationNo.Trim();
        model.HandheldDeviceId = string.IsNullOrWhiteSpace(model.HandheldDeviceId) ? null : model.HandheldDeviceId.Trim();
    }
}
