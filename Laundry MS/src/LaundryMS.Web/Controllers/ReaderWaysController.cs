using LaundryMS.Web.Data;
using LaundryMS.Web.Models.Entities;
using LaundryMS.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Controllers;

public class ReaderWaysController : TenantScopedController
{
    private readonly LaundryMsDbContext _dbContext;

    public ReaderWaysController(LaundryMsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(
        string? q,
        ulong? readerId,
        ulong? fromLocationId,
        ulong? toLocationId,
        string? businessPurposeKey,
        string? movementDirection,
        string? targetProcessStatus,
        bool includeInactive = false,
        bool onlySilent = false,
        bool onlyMissingEndpoints = false,
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

        var since24 = DateTime.UtcNow.AddHours(-24);
        var since7d = DateTime.UtcNow.AddDays(-7);

        var query = _dbContext.ReaderWays.AsNoTracking().Where(x => x.CustomerId == customerId).AsQueryable();
        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.WayName.Contains(term)
                || x.Reader.ReaderName.Contains(term)
                || x.BusinessPurposeKey.Contains(term));
        }

        if (readerId.HasValue)
            query = query.Where(x => x.ReaderId == readerId.Value);

        if (fromLocationId.HasValue)
            query = query.Where(x => x.FromLocationId == fromLocationId.Value);

        if (toLocationId.HasValue)
            query = query.Where(x => x.ToLocationId == toLocationId.Value);

        if (!string.IsNullOrWhiteSpace(businessPurposeKey))
        {
            var pk = businessPurposeKey.Trim().ToLowerInvariant();
            query = query.Where(x => x.BusinessPurposeKey == pk);
        }

        if (!string.IsNullOrWhiteSpace(movementDirection))
        {
            var md = movementDirection.Trim().ToLowerInvariant();
            query = query.Where(x => x.MovementDirection == md);
        }

        if (!string.IsNullOrWhiteSpace(targetProcessStatus))
        {
            var ts = targetProcessStatus.Trim().ToLowerInvariant();
            query = query.Where(x => x.TargetProcessStatus == ts);
        }

        if (onlySilent)
        {
            query = query.Where(x =>
                !_dbContext.LinenMovementEvents.Any(e =>
                    e.CustomerId == customerId && e.ReaderWayId == x.Id && e.OccurredAt >= since7d));
        }

        if (onlyMissingEndpoints)
            query = query.Where(x => x.FromLocationId == null || x.ToLocationId == null);

        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "name" : sortBy.Trim().ToLowerInvariant();
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = sortBy switch
        {
            "reader" => desc
                ? query.OrderByDescending(x => x.Reader.ReaderName).ThenBy(x => x.WayName)
                : query.OrderBy(x => x.Reader.ReaderName).ThenBy(x => x.WayName),
            "purpose" => desc
                ? query.OrderByDescending(x => x.BusinessPurposeKey).ThenBy(x => x.WayName)
                : query.OrderBy(x => x.BusinessPurposeKey).ThenBy(x => x.WayName),
            "direction" => desc
                ? query.OrderByDescending(x => x.MovementDirection).ThenBy(x => x.WayName)
                : query.OrderBy(x => x.MovementDirection).ThenBy(x => x.WayName),
            "lastscan" => desc
                ? query.OrderByDescending(x =>
                        _dbContext.LinenMovementEvents.Where(e => e.ReaderWayId == x.Id)
                            .Where(e => e.CustomerId == customerId)
                            .Max(e => (DateTime?)e.OccurredAt))
                    .ThenBy(x => x.WayName)
                : query.OrderBy(x =>
                        _dbContext.LinenMovementEvents.Where(e => e.ReaderWayId == x.Id)
                            .Where(e => e.CustomerId == customerId)
                            .Max(e => (DateTime?)e.OccurredAt))
                    .ThenBy(x => x.WayName),
            "scans24h" => desc
                ? query.OrderByDescending(x =>
                        _dbContext.LinenMovementEvents.Count(e =>
                            e.CustomerId == customerId && e.ReaderWayId == x.Id && e.OccurredAt >= since24))
                    .ThenBy(x => x.WayName)
                : query.OrderBy(x =>
                        _dbContext.LinenMovementEvents.Count(e =>
                            e.CustomerId == customerId && e.ReaderWayId == x.Id && e.OccurredAt >= since24))
                    .ThenBy(x => x.WayName),
            "openjobs" => desc
                ? query.OrderByDescending(x =>
                        _dbContext.LogisticsJobs.Count(j =>
                            j.CustomerId == customerId
                            &&
                            j.ReaderWayId == x.Id
                            && (j.JobStatus == "open" || j.JobStatus == "in_progress")))
                    .ThenBy(x => x.WayName)
                : query.OrderBy(x =>
                        _dbContext.LogisticsJobs.Count(j =>
                            j.CustomerId == customerId
                            &&
                            j.ReaderWayId == x.Id
                            && (j.JobStatus == "open" || j.JobStatus == "in_progress")))
                    .ThenBy(x => x.WayName),
            "status" => desc
                ? query.OrderByDescending(x => x.IsActive).ThenBy(x => x.WayName)
                : query.OrderBy(x => x.IsActive).ThenBy(x => x.WayName),
            "updated" => desc
                ? query.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.WayName)
                : query.OrderBy(x => x.UpdatedAt).ThenBy(x => x.WayName),
            _ => desc
                ? query.OrderByDescending(x => x.WayName)
                : query.OrderBy(x => x.WayName)
        };

        var total = await query.CountAsync(cancellationToken);
        var activeCount = await query.CountAsync(x => x.IsActive, cancellationToken);
        var silentCount = await query.CountAsync(cancellationToken: cancellationToken, predicate: x =>
            !_dbContext.LinenMovementEvents.Any(e => e.CustomerId == customerId && e.ReaderWayId == x.Id && e.OccurredAt >= since7d));

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.WayName,
                ReaderName = x.Reader.ReaderName,
                x.MovementDirection,
                x.BusinessPurposeKey,
                x.FromLocationId,
                x.ToLocationId,
                x.TargetProcessStatus,
                x.AntennaIndex,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var ids = rows.Select(x => x.Id).ToList();
        var locationNameMap = await _dbContext.Locations.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .ToDictionaryAsync(x => x.Id, x => x.LocationName, cancellationToken);

        var lastScan = ids.Count == 0
            ? new Dictionary<ulong, DateTime?>()
            : await _dbContext.LinenMovementEvents.AsNoTracking()
                .Where(e => e.CustomerId == customerId && ids.Contains(e.ReaderWayId))
                .GroupBy(e => e.ReaderWayId)
                .Select(g => new { Id = g.Key, Last = (DateTime?)g.Max(x => x.OccurredAt) })
                .ToDictionaryAsync(x => x.Id, x => x.Last, cancellationToken);

        var scans24h = ids.Count == 0
            ? new Dictionary<ulong, int>()
            : await _dbContext.LinenMovementEvents.AsNoTracking()
                .Where(e => e.CustomerId == customerId && ids.Contains(e.ReaderWayId) && e.OccurredAt >= since24)
                .GroupBy(e => e.ReaderWayId)
                .Select(g => new { Id = g.Key, C = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);

        var scans7d = ids.Count == 0
            ? new Dictionary<ulong, int>()
            : await _dbContext.LinenMovementEvents.AsNoTracking()
                .Where(e => e.CustomerId == customerId && ids.Contains(e.ReaderWayId) && e.OccurredAt >= since7d)
                .GroupBy(e => e.ReaderWayId)
                .Select(g => new { Id = g.Key, C = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);

        var exceptions7d = ids.Count == 0
            ? new Dictionary<ulong, int>()
            : await _dbContext.LinenMovementEvents.AsNoTracking()
                .Where(e => ids.Contains(e.ReaderWayId) && e.OccurredAt >= since7d
                    && e.CustomerId == customerId
                    && e.ProcessingResult.ToLower() != "accepted")
                .GroupBy(e => e.ReaderWayId)
                .Select(g => new { Id = g.Key, C = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);

        var openJobs = ids.Count == 0
            ? new Dictionary<ulong, int>()
            : await _dbContext.LogisticsJobs.AsNoTracking()
                .Where(j => j.ReaderWayId != null && ids.Contains(j.ReaderWayId.Value)
                    && j.CustomerId == customerId
                    && (j.JobStatus == "open" || j.JobStatus == "in_progress"))
                .GroupBy(j => j.ReaderWayId!.Value)
                .Select(g => new { Id = g.Key, C = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);

        var now = DateTime.UtcNow;
        var items = rows.Select(x =>
        {
            var last = lastScan.GetValueOrDefault(x.Id);
            var state = last == null
                ? "never_scanned"
                : (now - last.Value).TotalMinutes <= 10
                    ? "active"
                    : (now - last.Value).TotalDays <= 2
                        ? "quiet"
                        : "silent";

            return new ReaderWayListItemViewModel
            {
                Id = x.Id,
                WayName = x.WayName,
                ReaderName = x.ReaderName,
                MovementDirection = x.MovementDirection,
                BusinessPurposeKey = x.BusinessPurposeKey,
                FromLocationId = x.FromLocationId,
                ToLocationId = x.ToLocationId,
                FromLocationName = x.FromLocationId.HasValue
                    ? locationNameMap.GetValueOrDefault(x.FromLocationId.Value, "N/A")
                    : "—",
                ToLocationName = x.ToLocationId.HasValue
                    ? locationNameMap.GetValueOrDefault(x.ToLocationId.Value, "N/A")
                    : "—",
                TargetProcessStatus = x.TargetProcessStatus,
                AntennaIndex = x.AntennaIndex,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                LastScanAt = last,
                Scans24h = scans24h.GetValueOrDefault(x.Id),
                Scans7d = scans7d.GetValueOrDefault(x.Id),
                ExceptionScans7d = exceptions7d.GetValueOrDefault(x.Id),
                OpenJobsCount = openJobs.GetValueOrDefault(x.Id),
                ActivityState = state
            };
        }).ToList();

        var readers = await _dbContext.Readers.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderBy(x => x.ReaderCategory)
            .ThenBy(x => x.ReaderName)
            .Select(x => new ReaderOptionViewModel { Id = x.Id, ReaderName = x.ReaderName })
            .ToListAsync(cancellationToken);

        var locations = await _dbContext.Locations.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderBy(x => x.LocationName)
            .Select(x => new LocationOptionViewModel { Id = x.Id, LocationName = x.LocationName })
            .ToListAsync(cancellationToken);

        var purposes = await _dbContext.ReaderWays.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .Select(x => x.BusinessPurposeKey)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        var targetStatuses = await _dbContext.ReaderWays.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .Select(x => x.TargetProcessStatus)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        return View(new ReaderWaysIndexViewModel
        {
            Items = items,
            Readers = readers,
            Locations = locations,
            PurposeOptions = purposes,
            TargetStatusOptions = targetStatuses,
            Query = new ReaderWaysQueryViewModel
            {
                Q = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
                ReaderId = readerId,
                FromLocationId = fromLocationId,
                ToLocationId = toLocationId,
                BusinessPurposeKey = string.IsNullOrWhiteSpace(businessPurposeKey)
                    ? null
                    : businessPurposeKey.Trim().ToLowerInvariant(),
                MovementDirection = string.IsNullOrWhiteSpace(movementDirection)
                    ? null
                    : movementDirection.Trim().ToLowerInvariant(),
                TargetProcessStatus = string.IsNullOrWhiteSpace(targetProcessStatus)
                    ? null
                    : targetProcessStatus.Trim().ToLowerInvariant(),
                IncludeInactive = includeInactive,
                OnlySilent = onlySilent,
                OnlyMissingEndpoints = onlyMissingEndpoints,
                SortBy = sortBy,
                SortDir = desc ? "desc" : "asc"
            },
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            ActiveCount = activeCount,
            SilentCount = silentCount
        });
    }

    public async Task<IActionResult> Details(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var row = await _dbContext.ReaderWays.AsNoTracking()
            .Where(x => x.Id == id && x.CustomerId == customerId)
            .Select(x => new
            {
                x.Id,
                x.WayName,
                x.ReaderId,
                ReaderName = x.Reader.ReaderName,
                x.MovementDirection,
                x.BusinessPurposeKey,
                x.FromLocationId,
                FromLocationName = x.FromLocation != null ? x.FromLocation.LocationName : null,
                x.ToLocationId,
                ToLocationName = x.ToLocation != null ? x.ToLocation.LocationName : null,
                x.TargetProcessStatus,
                x.AntennaIndex,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row == null)
            return NotFound();

        var since24 = DateTime.UtcNow.AddHours(-24);
        var since7d = DateTime.UtcNow.AddDays(-7);

        var lastScan = await _dbContext.LinenMovementEvents.AsNoTracking()
            .Where(e => e.CustomerId == customerId && e.ReaderWayId == id)
            .MaxAsync(e => (DateTime?)e.OccurredAt, cancellationToken);
        var scans24h = await _dbContext.LinenMovementEvents.AsNoTracking()
            .CountAsync(e => e.CustomerId == customerId && e.ReaderWayId == id && e.OccurredAt >= since24, cancellationToken);
        var scans7d = await _dbContext.LinenMovementEvents.AsNoTracking()
            .CountAsync(e => e.CustomerId == customerId && e.ReaderWayId == id && e.OccurredAt >= since7d, cancellationToken);
        var exceptions7d = await _dbContext.LinenMovementEvents.AsNoTracking()
            .CountAsync(e => e.CustomerId == customerId && e.ReaderWayId == id && e.OccurredAt >= since7d
                && e.ProcessingResult.ToLower() != "accepted", cancellationToken);
        var openJobs = await _dbContext.LogisticsJobs.AsNoTracking()
            .CountAsync(j => j.CustomerId == customerId && j.ReaderWayId == id
                && (j.JobStatus == "open" || j.JobStatus == "in_progress"), cancellationToken);

        var recentEvents = await _dbContext.ReaderWayEvents.AsNoTracking()
            .Where(e => e.CustomerId == customerId && e.ReaderWayId == id)
            .OrderByDescending(e => e.CreatedAt)
            .Take(25)
            .Select(e => new ReaderWayEventRowViewModel
            {
                EventType = e.EventType,
                Note = e.Note,
                ChangedBy = e.ChangedBy,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var model = new ReaderWayDetailViewModel
        {
            Id = row.Id,
            WayName = row.WayName,
            ReaderId = row.ReaderId,
            ReaderName = row.ReaderName,
            MovementDirection = row.MovementDirection,
            BusinessPurposeKey = row.BusinessPurposeKey,
            FromLocationId = row.FromLocationId,
            FromLocationName = row.FromLocationName,
            ToLocationId = row.ToLocationId,
            ToLocationName = row.ToLocationName,
            TargetProcessStatus = row.TargetProcessStatus,
            AntennaIndex = row.AntennaIndex,
            IsActive = row.IsActive,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt,
            LastScanAt = lastScan,
            Scans24h = scans24h,
            Scans7d = scans7d,
            ExceptionScans7d = exceptions7d,
            OpenJobsCount = openJobs,
            RecentEvents = recentEvents
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Get(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        ReaderWayFormViewModel? way;
        try
        {
            way = await _dbContext.ReaderWays
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Where(x => x.CustomerId == customerId)
                .Select(x => new ReaderWayFormViewModel
                {
                    Id = x.Id,
                    ReaderId = x.ReaderId,
                    WayName = x.WayName,
                    MovementDirection = x.MovementDirection,
                    BusinessPurposeKey = x.BusinessPurposeKey,
                    FromLocationId = x.FromLocationId,
                    ToLocationId = x.ToLocationId,
                    TargetProcessStatus = x.TargetProcessStatus,
                    AntennaIndex = x.AntennaIndex,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Json(new { ok = false, message = "Request was canceled." });
        }

        if (way == null)
            return Json(new { ok = false, message = "Scan route not found." });

        return Json(new { ok = true, way });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReaderWayFormViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        if (cancellationToken.IsCancellationRequested)
            return Json(new { ok = false, message = "Request was canceled." });

        model = NormalizeForm(model);
        ModelState.Remove(nameof(ReaderWayFormViewModel.Id));
        TryValidateModel(model);

        if (!ModelState.IsValid)
            return Json(new { ok = false, errors = ModelState.ToDictionary(k => k.Key, v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? []) });

        bool readerExists;
        try
        {
            readerExists = await _dbContext.Readers.AnyAsync(x => x.Id == model.ReaderId && x.CustomerId == customerId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Json(new { ok = false, message = "Request was canceled." });
        }

        if (!readerExists)
            return Json(new { ok = false, errors = new Dictionary<string, string[]> { [nameof(ReaderWayFormViewModel.ReaderId)] = ["Reader not found."] } });

        if (model.FromLocationId.HasValue)
        {
            bool fromExists;
            try
            {
                fromExists = await _dbContext.Locations.AnyAsync(x => x.Id == model.FromLocationId.Value && x.CustomerId == customerId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return Json(new { ok = false, message = "Request was canceled." });
            }

            if (!fromExists)
                return Json(new { ok = false, errors = new Dictionary<string, string[]> { [nameof(ReaderWayFormViewModel.FromLocationId)] = ["From location not found."] } });
        }

        if (model.ToLocationId.HasValue)
        {
            bool toExists;
            try
            {
                toExists = await _dbContext.Locations.AnyAsync(x => x.Id == model.ToLocationId.Value && x.CustomerId == customerId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return Json(new { ok = false, message = "Request was canceled." });
            }

            if (!toExists)
                return Json(new { ok = false, errors = new Dictionary<string, string[]> { [nameof(ReaderWayFormViewModel.ToLocationId)] = ["To location not found."] } });
        }

        try
        {
            var dupeNamePerReader = await _dbContext.ReaderWays
                .AsNoTracking()
                .AnyAsync(x => x.CustomerId == customerId && x.ReaderId == model.ReaderId && x.WayName == model.WayName, cancellationToken);

            if (dupeNamePerReader)
            {
                return Json(new
                {
                    ok = false,
                    errors = new Dictionary<string, string[]>
                    {
                        [nameof(ReaderWayFormViewModel.WayName)] = ["Way name already exists for this reader."]
                    }
                });
            }
        }
        catch (OperationCanceledException)
        {
            return Json(new { ok = false, message = "Request was canceled." });
        }

        try
        {
            var dupeAntenna = await _dbContext.ReaderWays
                .AsNoTracking()
                .AnyAsync(
                    x => x.CustomerId == customerId && x.ReaderId == model.ReaderId && x.AntennaIndex == model.AntennaIndex,
                    cancellationToken);

            if (dupeAntenna)
            {
                return Json(new
                {
                    ok = false,
                    errors = new Dictionary<string, string[]>
                    {
                        [nameof(ReaderWayFormViewModel.AntennaIndex)] =
                            ["This antenna index is already mapped for this reader."]
                    }
                });
            }
        }
        catch (OperationCanceledException)
        {
            return Json(new { ok = false, message = "Request was canceled." });
        }

        var entity = new ReaderWay
        {
            CustomerId = customerId,
            ReaderId = model.ReaderId,
            WayName = model.WayName,
            MovementDirection = model.MovementDirection,
            BusinessPurposeKey = model.BusinessPurposeKey,
            FromLocationId = model.FromLocationId,
            ToLocationId = model.ToLocationId,
            TargetProcessStatus = model.TargetProcessStatus,
            AntennaIndex = model.AntennaIndex,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.ReaderWays.Add(entity);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _dbContext.ReaderWayEvents.Add(new ReaderWayEvent
            {
                CustomerId = customerId,
                ReaderWayId = entity.Id,
                EventType = "created",
                ChangedBy = User.Identity?.Name,
                CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Json(new { ok = true });
        }
        catch (DbUpdateException ex) when (IsUniqueAntennaViolation(ex))
        {
            return Json(new
            {
                ok = false,
                errors = new Dictionary<string, string[]>
                {
                    [nameof(ReaderWayFormViewModel.AntennaIndex)] =
                        ["Antenna index already mapped for this reader."]
                }
            });
        }
        catch (DbUpdateException ex) when (IsUniqueNamePerReaderViolation(ex))
        {
            return Json(new
            {
                ok = false,
                errors = new Dictionary<string, string[]>
                {
                    [nameof(ReaderWayFormViewModel.WayName)] = ["Way name already exists for this reader."]
                }
            });
        }
        catch (OperationCanceledException)
        {
            return Json(new { ok = false, message = "Request was canceled." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ReaderWayFormViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        if (cancellationToken.IsCancellationRequested)
            return Json(new { ok = false, message = "Request was canceled." });

        model = NormalizeForm(model);
        if (model.Id == null || model.Id == 0)
            return Json(new { ok = false, message = "Missing id." });

        ModelState.Remove(nameof(ReaderWayFormViewModel.Id));
        TryValidateModel(model);

        if (!ModelState.IsValid)
            return Json(new { ok = false, errors = ModelState.ToDictionary(k => k.Key, v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? []) });

        ReaderWay? entity;
        try
        {
            entity = await _dbContext.ReaderWays.FirstOrDefaultAsync(x => x.Id == model.Id && x.CustomerId == customerId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Json(new { ok = false, message = "Request was canceled." });
        }

        if (entity == null)
            return Json(new { ok = false, message = "Scan route not found." });

        bool readerExists;
        try
        {
            readerExists = await _dbContext.Readers.AnyAsync(x => x.Id == model.ReaderId && x.CustomerId == customerId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Json(new { ok = false, message = "Request was canceled." });
        }

        if (!readerExists)
            return Json(new { ok = false, errors = new Dictionary<string, string[]> { [nameof(ReaderWayFormViewModel.ReaderId)] = ["Reader not found."] } });

        if (model.FromLocationId.HasValue)
        {
            bool fromExists;
            try
            {
                fromExists = await _dbContext.Locations.AnyAsync(x => x.Id == model.FromLocationId.Value && x.CustomerId == customerId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return Json(new { ok = false, message = "Request was canceled." });
            }

            if (!fromExists)
                return Json(new { ok = false, errors = new Dictionary<string, string[]> { [nameof(ReaderWayFormViewModel.FromLocationId)] = ["From location not found."] } });
        }

        if (model.ToLocationId.HasValue)
        {
            bool toExists;
            try
            {
                toExists = await _dbContext.Locations.AnyAsync(x => x.Id == model.ToLocationId.Value && x.CustomerId == customerId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return Json(new { ok = false, message = "Request was canceled." });
            }

            if (!toExists)
                return Json(new { ok = false, errors = new Dictionary<string, string[]> { [nameof(ReaderWayFormViewModel.ToLocationId)] = ["To location not found."] } });
        }

        try
        {
            var dupeNamePerReader = await _dbContext.ReaderWays
                .AsNoTracking()
                .AnyAsync(x => x.CustomerId == customerId && x.Id != entity.Id && x.ReaderId == model.ReaderId && x.WayName == model.WayName, cancellationToken);

            if (dupeNamePerReader)
            {
                return Json(new
                {
                    ok = false,
                    errors = new Dictionary<string, string[]>
                    {
                        [nameof(ReaderWayFormViewModel.WayName)] = ["Way name already exists for this reader."]
                    }
                });
            }
        }
        catch (OperationCanceledException)
        {
            return Json(new { ok = false, message = "Request was canceled." });
        }

        try
        {
            var dupeAntenna = await _dbContext.ReaderWays
                .AsNoTracking()
                .AnyAsync(
                    x => x.CustomerId == customerId
                         && x.Id != entity.Id
                         && x.ReaderId == model.ReaderId
                         && x.AntennaIndex == model.AntennaIndex,
                    cancellationToken);

            if (dupeAntenna)
            {
                return Json(new
                {
                    ok = false,
                    errors = new Dictionary<string, string[]>
                    {
                        [nameof(ReaderWayFormViewModel.AntennaIndex)] =
                            ["This antenna index is already mapped for this reader."]
                    }
                });
            }
        }
        catch (OperationCanceledException)
        {
            return Json(new { ok = false, message = "Request was canceled." });
        }

        entity.ReaderId = model.ReaderId;
        entity.WayName = model.WayName;
        entity.MovementDirection = model.MovementDirection;
        entity.BusinessPurposeKey = model.BusinessPurposeKey;
        entity.FromLocationId = model.FromLocationId;
        entity.ToLocationId = model.ToLocationId;
        entity.TargetProcessStatus = model.TargetProcessStatus;
        entity.AntennaIndex = model.AntennaIndex;
        entity.IsActive = model.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _dbContext.ReaderWayEvents.Add(new ReaderWayEvent
            {
                CustomerId = customerId,
                ReaderWayId = entity.Id,
                EventType = "updated",
                ChangedBy = User.Identity?.Name,
                CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Json(new { ok = true });
        }
        catch (DbUpdateException ex) when (IsUniqueAntennaViolation(ex))
        {
            return Json(new
            {
                ok = false,
                errors = new Dictionary<string, string[]>
                {
                    [nameof(ReaderWayFormViewModel.AntennaIndex)] =
                        ["Antenna index already mapped for this reader."]
                }
            });
        }
        catch (DbUpdateException ex) when (IsUniqueNamePerReaderViolation(ex))
        {
            return Json(new
            {
                ok = false,
                errors = new Dictionary<string, string[]>
                {
                    [nameof(ReaderWayFormViewModel.WayName)] = ["Way name already exists for this reader."]
                }
            });
        }
        catch (OperationCanceledException)
        {
            return Json(new { ok = false, message = "Request was canceled." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        ReaderWay? entity;
        try
        {
            entity = await _dbContext.ReaderWays.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Json(new { ok = false, message = "Request was canceled." });
        }

        if (entity == null)
            return Json(new { ok = false, message = "Scan route not found." });

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        _dbContext.ReaderWayEvents.Add(new ReaderWayEvent
        {
            CustomerId = customerId,
            ReaderWayId = id,
            EventType = "deactivated",
            ChangedBy = User.Identity?.Name,
            CreatedAt = DateTime.UtcNow
        });
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Json(new { ok = true });
        }
        catch (OperationCanceledException)
        {
            return Json(new { ok = false, message = "Request was canceled." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        ReaderWay? entity;
        try
        {
            entity = await _dbContext.ReaderWays.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Json(new { ok = false, message = "Request was canceled." });
        }

        if (entity == null)
            return Json(new { ok = false, message = "Scan route not found." });

        entity.IsActive = true;
        entity.UpdatedAt = DateTime.UtcNow;
        _dbContext.ReaderWayEvents.Add(new ReaderWayEvent
        {
            CustomerId = customerId,
            ReaderWayId = id,
            EventType = "activated",
            ChangedBy = User.Identity?.Name,
            CreatedAt = DateTime.UtcNow
        });
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Json(new { ok = true });
        }
        catch (OperationCanceledException)
        {
            return Json(new { ok = false, message = "Request was canceled." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        ReaderWay? entity;
        try
        {
            entity = await _dbContext.ReaderWays.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Json(new { ok = false, message = "Request was canceled." });
        }

        if (entity == null)
            return Json(new { ok = false, message = "Scan route not found." });

        bool hasScans;
        try
        {
            hasScans = await _dbContext.LinenMovementEvents.AnyAsync(x => x.CustomerId == customerId && x.ReaderWayId == id, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Json(new { ok = false, message = "Request was canceled." });
        }

        if (hasScans)
            return Json(new { ok = false, message = "Cannot delete: this scan route is referenced by scan history." });

        bool hasJobs;
        try
        {
            hasJobs = await _dbContext.LogisticsJobs.AnyAsync(x => x.CustomerId == customerId && x.ReaderWayId == id, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Json(new { ok = false, message = "Request was canceled." });
        }

        if (hasJobs)
            return Json(new { ok = false, message = "Cannot delete: this scan route is assigned to logistics jobs." });

        _dbContext.ReaderWays.Remove(entity);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Json(new { ok = true });
        }
        catch (OperationCanceledException)
        {
            return Json(new { ok = false, message = "Request was canceled." });
        }
    }

    private static ReaderWayFormViewModel NormalizeForm(ReaderWayFormViewModel model)
    {
        model.WayName = (model.WayName ?? string.Empty).Trim();
        model.MovementDirection = string.IsNullOrWhiteSpace(model.MovementDirection) ? "in" : model.MovementDirection.Trim().ToLowerInvariant();
        if (model.MovementDirection is not ("in" or "out" or "enter" or "exit"))
            model.MovementDirection = "in";

        model.BusinessPurposeKey = string.IsNullOrWhiteSpace(model.BusinessPurposeKey) ? "scan" : model.BusinessPurposeKey.Trim().ToLowerInvariant();
        model.TargetProcessStatus = string.IsNullOrWhiteSpace(model.TargetProcessStatus) ? "at_customer" : model.TargetProcessStatus.Trim().ToLowerInvariant();

        if (model.FromLocationId == 0) model.FromLocationId = null;
        if (model.ToLocationId == 0) model.ToLocationId = null;

        if (model.AntennaIndex < 0)
            model.AntennaIndex = 0;
        if (model.AntennaIndex > 16)
            model.AntennaIndex = 16;

        return model;
    }

    private static bool IsUniqueNamePerReaderViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message ?? string.Empty;
        return msg.Contains("uq_reader_ways_name_per_reader", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUniqueAntennaViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message ?? string.Empty;
        return msg.Contains("uq_reader_ways_antenna_per_reader", StringComparison.OrdinalIgnoreCase);
    }
}
