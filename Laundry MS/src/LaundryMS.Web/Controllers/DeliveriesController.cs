using LaundryMS.Web.Data;
using LaundryMS.Web.Models.Entities;
using LaundryMS.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Controllers;

public class DeliveriesController : TenantScopedController
{
    private readonly LaundryMsDbContext _dbContext;

    public DeliveriesController(LaundryMsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var customers = await _dbContext.Customers
            .AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == customerId)
            .OrderBy(x => x.CustomerName)
            .Select(x => new CustomerOptionViewModel
            {
                Id = x.Id,
                CustomerName = x.CustomerName
            })
            .ToListAsync(cancellationToken);

        var drivers = await _dbContext.Drivers
            .AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == customerId)
            .OrderBy(x => x.DriverName)
            .Select(x => new DriverOptionViewModel
            {
                Id = x.Id,
                DriverName = x.DriverName
            })
            .ToListAsync(cancellationToken);

        var locations = await _dbContext.Locations
            .AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == customerId)
            .OrderBy(x => x.LocationName)
            .Select(x => new LocationOptionViewModel
            {
                Id = x.Id,
                LocationName = x.LocationName
            })
            .ToListAsync(cancellationToken);

        var readerWays = await _dbContext.ReaderWays
            .AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == customerId)
            .OrderBy(x => x.WayName)
            .Select(x => new ReaderWayOptionViewModel
            {
                Id = x.Id,
                DisplayName = x.WayName + " — " + x.Reader.ReaderName
            })
            .ToListAsync(cancellationToken);

        var jobs = await LogisticsJobsListLoader.LoadAsync(
            _dbContext,
            cancellationToken,
            q => q.WhereDeliveryJobType().Where(x => x.CustomerId == customerId));

        return View(new DeliveriesIndexViewModel
        {
            Customers = customers,
            Drivers = drivers,
            Locations = locations,
            ReaderWays = readerWays,
            Jobs = jobs
        });
    }

    public async Task<IActionResult> Details(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var head = await _dbContext.LogisticsJobs
            .AsNoTracking()
            .Where(x => x.Id == id && x.CustomerId == customerId)
            .WhereDeliveryJobType()
            .Select(x => new
            {
                x.Id,
                x.JobType,
                x.JobStatus,
                CustomerName = _dbContext.Customers
                    .Where(c => c.CustomerId == x.CustomerId)
                    .OrderBy(c => c.CustomerName)
                    .Select(c => c.CustomerName)
                    .FirstOrDefault(),
                DriverName = x.Driver != null ? x.Driver.DriverName : null,
                FromLocationName = x.FromLocation != null ? x.FromLocation.LocationName : null,
                ToLocationName = x.ToLocation != null ? x.ToLocation.LocationName : null,
                ReaderWayName = x.ReaderWay != null ? x.ReaderWay.WayName : null,
                ReaderNameOnWay = x.ReaderWay != null ? x.ReaderWay.Reader.ReaderName : null,
                x.Notes,
                x.PlannedStartAt,
                x.PlannedEndAt,
                x.ActualStartAt,
                x.ActualEndAt,
                x.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (head == null)
            return NotFound();

        var expectedItemCount = await _dbContext.JobExpectedItems.AsNoTracking()
            .CountAsync(x => x.CustomerId == customerId && x.LogisticsJobId == id, cancellationToken);

        var reachedItemCount = await _dbContext.JobExpectedItems.AsNoTracking()
            .CountAsync(x => x.CustomerId == customerId && x.LogisticsJobId == id && x.ReachedExpectedStatus, cancellationToken);

        var scanAgg = await _dbContext.LinenMovementEvents.AsNoTracking()
            .Where(e => e.CustomerId == customerId && e.LogisticsJobId == id)
            .Select(e => new { e.LinenItemId, e.OccurredAt })
            .ToListAsync(cancellationToken);

        var scannedUnique = scanAgg.Select(x => x.LinenItemId).Distinct().Count();
        var lastScanAt = scanAgg.Count == 0 ? (DateTime?)null : scanAgg.Max(x => x.OccurredAt);

        var expectedItems = await _dbContext.JobExpectedItems
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.LogisticsJobId == id)
            .OrderBy(x => x.LinenItem.RfidTag)
            .Select(x => new DeliveryExpectedItemRowViewModel
            {
                JobExpectedItemId = x.Id,
                RfidTag = x.LinenItem.RfidTag,
                ItemType = x.LinenItem.ItemType,
                SizeLabel = x.LinenItem.SizeLabel,
                PhysicalCondition = x.LinenItem.PhysicalCondition,
                ExpectedProcessStatus = x.ExpectedProcessStatus,
                ReachedExpectedStatus = x.ReachedExpectedStatus,
                ReachedAt = x.ReachedAt
            })
            .ToListAsync(cancellationToken);

        var recentScans = await _dbContext.LinenMovementEvents
            .AsNoTracking()
            .Where(e => e.CustomerId == customerId && e.LogisticsJobId == id)
            .OrderByDescending(e => e.OccurredAt)
            .Take(400)
            .Select(e => new DeliveryScanEventRowViewModel
            {
                EventId = e.Id,
                RfidTag = e.LinenItem.RfidTag,
                ReaderName = e.Reader.ReaderName,
                WayName = e.ReaderWay.WayName,
                ProcessingResult = e.ProcessingResult,
                ConditionAfterEvent = e.ConditionAfterEvent,
                OccurredAt = e.OccurredAt
            })
            .ToListAsync(cancellationToken);

        var vm = new DeliveryJobDetailViewModel
        {
            Id = head.Id,
            JobType = head.JobType,
            JobStatus = head.JobStatus,
            CustomerName = head.CustomerName,
            DriverName = head.DriverName,
            FromLocationName = head.FromLocationName,
            ToLocationName = head.ToLocationName,
            ReaderWayName = head.ReaderWayName,
            ReaderNameOnWay = head.ReaderNameOnWay,
            Notes = head.Notes,
            PlannedStartAt = head.PlannedStartAt,
            PlannedEndAt = head.PlannedEndAt,
            ActualStartAt = head.ActualStartAt,
            ActualEndAt = head.ActualEndAt,
            CreatedAt = head.CreatedAt,
            ExpectedItemCount = expectedItemCount,
            ReachedItemCount = reachedItemCount,
            ScannedUniqueTagCount = scannedUnique,
            LastScanAt = lastScanAt,
            ExpectedItems = expectedItems,
            RecentScans = recentScans
        };

        return View(vm);
    }

    public async Task<IActionResult> Print(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        // Print-friendly delivery list (RFID IDs). Uses expected items as the planned list, plus scans linked to this job.
        var vm = await BuildDetailAsync(id, customerId, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finalize(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var entity = await _dbContext.LogisticsJobs.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);
        if (entity == null || !IsAllowedDeliveryType(entity.JobType))
            return NotFound();

        entity.JobStatus = "completed";
        entity.ActualEndAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        TempData["DeliveriesToast"] = "Job marked complete.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartProgress(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var entity = await _dbContext.LogisticsJobs.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);
        if (entity == null || !IsAllowedDeliveryType(entity.JobType))
            return NotFound();

        entity.JobStatus = "in_progress";
        entity.ActualStartAt ??= DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        TempData["DeliveriesToast"] = "Job started.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Get(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var job = await _dbContext.LogisticsJobs
            .AsNoTracking()
            .Where(x => x.Id == id && x.CustomerId == customerId)
            .WhereDeliveryJobType()
            .Select(x => new LogisticsJobFormViewModel
            {
                Id = x.Id,
                JobType = x.JobType,
                CustomerId = x.CustomerId,
                DriverId = x.DriverId,
                FromLocationId = x.FromLocationId,
                ToLocationId = x.ToLocationId,
                ReaderWayId = x.ReaderWayId,
                JobStatus = x.JobStatus,
                PlannedStartAt = x.PlannedStartAt,
                PlannedEndAt = x.PlannedEndAt,
                ActualStartAt = x.ActualStartAt,
                ActualEndAt = x.ActualEndAt,
                Notes = x.Notes
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (job == null)
            return Json(new { ok = false, message = "Job not found." });

        return Json(new { ok = true, job });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LogisticsJobFormViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var tenantCustomerId))
            return Forbid();

        model = NormalizeForm(model);
        model.CustomerId = tenantCustomerId;
        ModelState.Remove(nameof(LogisticsJobFormViewModel.Id));
        TryValidateModel(model);

        if (!ModelState.IsValid)
            return Json(new { ok = false, errors = ModelState.ToDictionary(k => k.Key, v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? []) });

        if (!IsAllowedDeliveryType(model.JobType))
            return Json(new { ok = false, errors = new Dictionary<string, string[]> { [nameof(LogisticsJobFormViewModel.JobType)] = ["Job type must be delivery or dispatch."] } });

        var fkErrors = await ValidateOptionalForeignKeysAsync(model, cancellationToken);
        if (fkErrors.Count > 0)
            return Json(new { ok = false, errors = fkErrors });

        var entity = new LogisticsJob
        {
            JobType = model.JobType,
            CustomerId = model.CustomerId,
            DriverId = model.DriverId,
            FromLocationId = model.FromLocationId,
            ToLocationId = model.ToLocationId,
            ReaderWayId = model.ReaderWayId,
            JobStatus = model.JobStatus,
            PlannedStartAt = model.PlannedStartAt,
            PlannedEndAt = model.PlannedEndAt,
            ActualStartAt = model.ActualStartAt,
            ActualEndAt = model.ActualEndAt,
            Notes = model.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.LogisticsJobs.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(LogisticsJobFormViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var tenantCustomerId))
            return Forbid();

        model = NormalizeForm(model);
        model.CustomerId = tenantCustomerId;
        if (model.Id == null || model.Id == 0)
            return Json(new { ok = false, message = "Missing id." });

        ModelState.Remove(nameof(LogisticsJobFormViewModel.Id));
        TryValidateModel(model);

        if (!ModelState.IsValid)
            return Json(new { ok = false, errors = ModelState.ToDictionary(k => k.Key, v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? []) });

        if (!IsAllowedDeliveryType(model.JobType))
            return Json(new { ok = false, errors = new Dictionary<string, string[]> { [nameof(LogisticsJobFormViewModel.JobType)] = ["Job type must be delivery or dispatch."] } });

        var entity = await _dbContext.LogisticsJobs.FirstOrDefaultAsync(x => x.Id == model.Id && x.CustomerId == tenantCustomerId, cancellationToken);
        if (entity == null)
            return Json(new { ok = false, message = "Job not found." });

        if (!IsAllowedDeliveryType(entity.JobType))
            return Json(new { ok = false, message = "Job is not a delivery job." });

        var fkErrors = await ValidateOptionalForeignKeysAsync(model, cancellationToken);
        if (fkErrors.Count > 0)
            return Json(new { ok = false, errors = fkErrors });

        entity.JobType = model.JobType;
        entity.CustomerId = model.CustomerId;
        entity.DriverId = model.DriverId;
        entity.FromLocationId = model.FromLocationId;
        entity.ToLocationId = model.ToLocationId;
        entity.ReaderWayId = model.ReaderWayId;
        entity.JobStatus = model.JobStatus;
        entity.PlannedStartAt = model.PlannedStartAt;
        entity.PlannedEndAt = model.PlannedEndAt;
        entity.ActualStartAt = model.ActualStartAt;
        entity.ActualEndAt = model.ActualEndAt;
        entity.Notes = model.Notes;
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

        var entity = await _dbContext.LogisticsJobs.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);
        if (entity == null)
            return Json(new { ok = false, message = "Job not found." });

        if (!IsAllowedDeliveryType(entity.JobType))
            return Json(new { ok = false, message = "Job is not a delivery job." });

        var hasExpectedItems = await _dbContext.JobExpectedItems.AnyAsync(x => x.CustomerId == customerId && x.LogisticsJobId == id, cancellationToken);
        if (hasExpectedItems)
            return Json(new { ok = false, message = "Cannot delete: this job has expected items attached." });

        _dbContext.LogisticsJobs.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Json(new { ok = true });
    }

    private async Task<DeliveryJobDetailViewModel?> BuildDetailAsync(ulong id, ulong customerId, CancellationToken cancellationToken)
    {
        var head = await _dbContext.LogisticsJobs
            .AsNoTracking()
            .Where(x => x.Id == id && x.CustomerId == customerId)
            .WhereDeliveryJobType()
            .Select(x => new
            {
                x.Id,
                x.JobType,
                x.JobStatus,
                CustomerName = _dbContext.Customers
                    .Where(c => c.CustomerId == x.CustomerId)
                    .OrderBy(c => c.CustomerName)
                    .Select(c => c.CustomerName)
                    .FirstOrDefault(),
                DriverName = x.Driver != null ? x.Driver.DriverName : null,
                FromLocationName = x.FromLocation != null ? x.FromLocation.LocationName : null,
                ToLocationName = x.ToLocation != null ? x.ToLocation.LocationName : null,
                ReaderWayName = x.ReaderWay != null ? x.ReaderWay.WayName : null,
                ReaderNameOnWay = x.ReaderWay != null ? x.ReaderWay.Reader.ReaderName : null,
                x.Notes,
                x.PlannedStartAt,
                x.PlannedEndAt,
                x.ActualStartAt,
                x.ActualEndAt,
                x.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (head == null) return null;

        var expectedItemCount = await _dbContext.JobExpectedItems.AsNoTracking()
            .CountAsync(x => x.CustomerId == customerId && x.LogisticsJobId == id, cancellationToken);

        var reachedItemCount = await _dbContext.JobExpectedItems.AsNoTracking()
            .CountAsync(x => x.CustomerId == customerId && x.LogisticsJobId == id && x.ReachedExpectedStatus, cancellationToken);

        var scanAgg = await _dbContext.LinenMovementEvents.AsNoTracking()
            .Where(e => e.CustomerId == customerId && e.LogisticsJobId == id)
            .Select(e => new { e.LinenItemId, e.OccurredAt })
            .ToListAsync(cancellationToken);

        var scannedUnique = scanAgg.Select(x => x.LinenItemId).Distinct().Count();
        var lastScanAt = scanAgg.Count == 0 ? (DateTime?)null : scanAgg.Max(x => x.OccurredAt);

        var expectedItems = await _dbContext.JobExpectedItems
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.LogisticsJobId == id)
            .OrderBy(x => x.LinenItem.RfidTag)
            .Select(x => new DeliveryExpectedItemRowViewModel
            {
                JobExpectedItemId = x.Id,
                RfidTag = x.LinenItem.RfidTag,
                ItemType = x.LinenItem.ItemType,
                SizeLabel = x.LinenItem.SizeLabel,
                PhysicalCondition = x.LinenItem.PhysicalCondition,
                ExpectedProcessStatus = x.ExpectedProcessStatus,
                ReachedExpectedStatus = x.ReachedExpectedStatus,
                ReachedAt = x.ReachedAt
            })
            .ToListAsync(cancellationToken);

        var recentScans = await _dbContext.LinenMovementEvents
            .AsNoTracking()
            .Where(e => e.CustomerId == customerId && e.LogisticsJobId == id)
            .OrderByDescending(e => e.OccurredAt)
            .Take(2000)
            .Select(e => new DeliveryScanEventRowViewModel
            {
                EventId = e.Id,
                RfidTag = e.LinenItem.RfidTag,
                ReaderName = e.Reader.ReaderName,
                WayName = e.ReaderWay.WayName,
                ProcessingResult = e.ProcessingResult,
                ConditionAfterEvent = e.ConditionAfterEvent,
                OccurredAt = e.OccurredAt
            })
            .ToListAsync(cancellationToken);

        return new DeliveryJobDetailViewModel
        {
            Id = head.Id,
            JobType = head.JobType,
            JobStatus = head.JobStatus,
            CustomerName = head.CustomerName,
            DriverName = head.DriverName,
            FromLocationName = head.FromLocationName,
            ToLocationName = head.ToLocationName,
            ReaderWayName = head.ReaderWayName,
            ReaderNameOnWay = head.ReaderNameOnWay,
            Notes = head.Notes,
            PlannedStartAt = head.PlannedStartAt,
            PlannedEndAt = head.PlannedEndAt,
            ActualStartAt = head.ActualStartAt,
            ActualEndAt = head.ActualEndAt,
            CreatedAt = head.CreatedAt,
            ExpectedItemCount = expectedItemCount,
            ReachedItemCount = reachedItemCount,
            ScannedUniqueTagCount = scannedUnique,
            LastScanAt = lastScanAt,
            ExpectedItems = expectedItems,
            RecentScans = recentScans
        };
    }

    private async Task<Dictionary<string, string[]>> ValidateOptionalForeignKeysAsync(
        LogisticsJobFormViewModel model,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (model.CustomerId.HasValue)
        {
            var ok = await _dbContext.Customers.AnyAsync(x => x.CustomerId == model.CustomerId.Value, cancellationToken);
            if (!ok) errors[nameof(LogisticsJobFormViewModel.CustomerId)] = ["Customer not found."];
        }

        if (model.DriverId.HasValue)
        {
            var ok = await _dbContext.Drivers.AnyAsync(x => x.Id == model.DriverId.Value && x.CustomerId == model.CustomerId, cancellationToken);
            if (!ok) errors[nameof(LogisticsJobFormViewModel.DriverId)] = ["Driver not found."];
        }

        if (model.FromLocationId.HasValue)
        {
            var ok = await _dbContext.Locations.AnyAsync(x => x.Id == model.FromLocationId.Value && x.CustomerId == model.CustomerId, cancellationToken);
            if (!ok) errors[nameof(LogisticsJobFormViewModel.FromLocationId)] = ["Location not found."];
        }

        if (model.ToLocationId.HasValue)
        {
            var ok = await _dbContext.Locations.AnyAsync(x => x.Id == model.ToLocationId.Value && x.CustomerId == model.CustomerId, cancellationToken);
            if (!ok) errors[nameof(LogisticsJobFormViewModel.ToLocationId)] = ["Location not found."];
        }

        if (model.ReaderWayId.HasValue)
        {
            var ok = await _dbContext.ReaderWays.AnyAsync(x => x.Id == model.ReaderWayId.Value && x.CustomerId == model.CustomerId, cancellationToken);
            if (!ok) errors[nameof(LogisticsJobFormViewModel.ReaderWayId)] = ["Scan route not found."];
        }

        return errors;
    }

    private static LogisticsJobFormViewModel NormalizeForm(LogisticsJobFormViewModel model)
    {
        model.JobType = string.IsNullOrWhiteSpace(model.JobType) ? "delivery" : model.JobType.Trim().ToLowerInvariant();
        model.JobStatus = string.IsNullOrWhiteSpace(model.JobStatus) ? "open" : model.JobStatus.Trim().ToLowerInvariant();
        model.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();

        if (model.CustomerId == 0) model.CustomerId = null;
        if (model.DriverId == 0) model.DriverId = null;
        if (model.FromLocationId == 0) model.FromLocationId = null;
        if (model.ToLocationId == 0) model.ToLocationId = null;
        if (model.ReaderWayId == 0) model.ReaderWayId = null;

        return model;
    }

    private static bool IsAllowedDeliveryType(string? jobType)
    {
        if (string.IsNullOrWhiteSpace(jobType)) return false;
        var t = jobType.Trim().ToLowerInvariant();
        return t is "delivery" or "dispatch";
    }

}
