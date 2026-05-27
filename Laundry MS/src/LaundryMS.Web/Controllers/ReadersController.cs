using LaundryMS.Web.Data;
using LaundryMS.Web.Models.Entities;
using LaundryMS.Web.Models.ViewModels;
using LaundryMS.Web.Options;
using LaundryMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LaundryMS.Web.Controllers;

public class ReadersController : TenantScopedController
{
    private readonly LaundryMsDbContext _dbContext;
    private readonly IMqttReaderStateRegistry _mqttReaderRegistry;
    private readonly IOptions<MqttOptions> _mqttOptions;

    public ReadersController(
        LaundryMsDbContext dbContext,
        IMqttReaderStateRegistry mqttReaderRegistry,
        IOptions<MqttOptions> mqttOptions)
    {
        _dbContext = dbContext;
        _mqttReaderRegistry = mqttReaderRegistry;
        _mqttOptions = mqttOptions;
    }

    public async Task<IActionResult> Index(
        string? q,
        string? category,
        bool includeInactive = false,
        bool onlyUnmapped = false,
        bool onlyIdle = false,
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

        var query = _dbContext.Readers.AsNoTracking().Where(x => x.CustomerId == customerId);
        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.ReaderName.Contains(term)
                || x.DeviceIdentifier.Contains(term)
                || (x.DeviceModel != null && x.DeviceModel.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim().ToLowerInvariant();
            query = query.Where(x => x.ReaderCategory == cat);
        }

        if (onlyUnmapped)
            query = query.Where(x => !_dbContext.ReaderWays.Any(w => w.CustomerId == customerId && w.ReaderId == x.Id));

        if (onlyIdle)
        {
            var idleCutoff = DateTime.UtcNow.AddDays(-2);
            query = query.Where(x =>
                !_dbContext.LinenMovementEvents.Any(e => e.ReaderId == x.Id)
                || _dbContext.LinenMovementEvents
                    .Where(e => e.CustomerId == customerId && e.ReaderId == x.Id)
                    .Max(e => (DateTime?)e.OccurredAt) < idleCutoff);
        }

        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "name" : sortBy.Trim().ToLowerInvariant();
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        query = sortBy switch
        {
            "category" => desc
                ? query.OrderByDescending(x => x.ReaderCategory).ThenBy(x => x.ReaderName)
                : query.OrderBy(x => x.ReaderCategory).ThenBy(x => x.ReaderName),
            "routes" => desc
                ? query.OrderByDescending(x => _dbContext.ReaderWays.Count(w => w.CustomerId == customerId && w.ReaderId == x.Id)).ThenBy(x => x.ReaderName)
                : query.OrderBy(x => _dbContext.ReaderWays.Count(w => w.CustomerId == customerId && w.ReaderId == x.Id)).ThenBy(x => x.ReaderName),
            "status" => desc
                ? query.OrderByDescending(x => x.IsActive).ThenBy(x => x.ReaderName)
                : query.OrderBy(x => x.IsActive).ThenBy(x => x.ReaderName),
            "lastseen" => desc
                ? query.OrderByDescending(x => _dbContext.LinenMovementEvents.Where(e => e.CustomerId == customerId && e.ReaderId == x.Id).Max(e => (DateTime?)e.OccurredAt)).ThenBy(x => x.ReaderName)
                : query.OrderBy(x => _dbContext.LinenMovementEvents.Where(e => e.CustomerId == customerId && e.ReaderId == x.Id).Max(e => (DateTime?)e.OccurredAt)).ThenBy(x => x.ReaderName),
            _ => desc
                ? query.OrderByDescending(x => x.ReaderName)
                : query.OrderBy(x => x.ReaderName)
        };

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.ReaderName,
                x.DeviceIdentifier,
                x.DeviceModel,
                x.ReaderCategory,
                x.IsActive
            })
            .ToListAsync(cancellationToken);

        var ids = rows.Select(x => x.Id).ToList();
        var since24 = DateTime.UtcNow.AddHours(-24);
        var since7d = DateTime.UtcNow.AddDays(-7);
        var wayCounts = ids.Count == 0
            ? new Dictionary<ulong, int>()
            : await _dbContext.ReaderWays.AsNoTracking()
                .Where(w => w.CustomerId == customerId && ids.Contains(w.ReaderId))
                .GroupBy(w => w.ReaderId)
                .Select(g => new { Id = g.Key, C = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);

        var lastSeen = ids.Count == 0
            ? new Dictionary<ulong, DateTime?>()
            : await _dbContext.LinenMovementEvents.AsNoTracking()
                .Where(e => e.CustomerId == customerId && ids.Contains(e.ReaderId))
                .GroupBy(e => e.ReaderId)
                .Select(g => new { Id = g.Key, Last = (DateTime?)g.Max(x => x.OccurredAt) })
                .ToDictionaryAsync(x => x.Id, x => x.Last, cancellationToken);

        var scans24h = ids.Count == 0
            ? new Dictionary<ulong, int>()
            : await _dbContext.LinenMovementEvents.AsNoTracking()
                .Where(e => e.CustomerId == customerId && ids.Contains(e.ReaderId) && e.OccurredAt >= since24)
                .GroupBy(e => e.ReaderId)
                .Select(g => new { Id = g.Key, C = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);

        var scans7d = ids.Count == 0
            ? new Dictionary<ulong, int>()
            : await _dbContext.LinenMovementEvents.AsNoTracking()
                .Where(e => e.CustomerId == customerId && ids.Contains(e.ReaderId) && e.OccurredAt >= since7d)
                .GroupBy(e => e.ReaderId)
                .Select(g => new { Id = g.Key, C = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.C, cancellationToken);

        var now = DateTime.UtcNow;
        var items = rows.Select(x =>
        {
            var seen = lastSeen.GetValueOrDefault(x.Id);
            var state = seen == null ? "never_seen"
                : (now - seen.Value).TotalMinutes <= 10 ? "online"
                : (now - seen.Value).TotalDays <= 2 ? "idle"
                : "offline";
            var wc = wayCounts.GetValueOrDefault(x.Id);
            return new ReaderListItemViewModel
            {
                Id = x.Id,
                ReaderName = x.ReaderName,
                DeviceIdentifier = x.DeviceIdentifier,
                DeviceModel = x.DeviceModel,
                ReaderCategory = x.ReaderCategory,
                IsActive = x.IsActive,
                WayCount = wc,
                LastSeenAt = seen,
                Scans24h = scans24h.GetValueOrDefault(x.Id),
                Scans7d = scans7d.GetValueOrDefault(x.Id),
                OnlineState = state,
                CoverageSummary = wc == 1 ? "1 route" : $"{wc} routes"
            };
        }).ToList();

        var categories = await _dbContext.Readers.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .Select(x => x.ReaderCategory)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        return View(new ReadersIndexViewModel
        {
            Items = items,
            CategoryOptions = categories,
            Query = new ReadersQueryViewModel
            {
                Q = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
                Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim().ToLowerInvariant(),
                IncludeInactive = includeInactive,
                OnlyUnmapped = onlyUnmapped,
                OnlyIdle = onlyIdle,
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

        var reader = await _dbContext.Readers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);
        if (reader == null)
            return NotFound();

        var since24 = DateTime.UtcNow.AddHours(-24);
        var since7d = DateTime.UtcNow.AddDays(-7);

        var lastSeen = await _dbContext.LinenMovementEvents.AsNoTracking()
            .Where(e => e.CustomerId == customerId && e.ReaderId == id)
            .MaxAsync(e => (DateTime?)e.OccurredAt, cancellationToken);
        var scans24h = await _dbContext.LinenMovementEvents.AsNoTracking()
            .CountAsync(e => e.CustomerId == customerId && e.ReaderId == id && e.OccurredAt >= since24, cancellationToken);
        var scans7d = await _dbContext.LinenMovementEvents.AsNoTracking()
            .CountAsync(e => e.CustomerId == customerId && e.ReaderId == id && e.OccurredAt >= since7d, cancellationToken);
        var routeCount = await _dbContext.ReaderWays.AsNoTracking()
            .CountAsync(x => x.CustomerId == customerId && x.ReaderId == id, cancellationToken);

        var prefix = _mqttOptions.Value.TopicPrefix.Trim().Trim('/');
        bool? mqttOnline = null;
        if (_mqttReaderRegistry.TryGetConnectionState(customerId, reader.DeviceIdentifier, out var mos))
            mqttOnline = mos;

        var dev = reader.DeviceIdentifier;

        var model = new ReaderDetailViewModel
        {
            Id = reader.Id,
            ReaderName = reader.ReaderName,
            DeviceIdentifier = reader.DeviceIdentifier,
            DeviceModel = reader.DeviceModel,
            ReaderCategory = reader.ReaderCategory,
            IsActive = reader.IsActive,
            InstalledAt = reader.InstalledAt,
            LastHeartbeatAt = reader.LastHeartbeatAt,
            MaintenanceNote = reader.MaintenanceNote,
            CreatedAt = reader.CreatedAt,
            UpdatedAt = reader.UpdatedAt,
            LastSeenAt = lastSeen,
            Scans24h = scans24h,
            Scans7d = scans7d,
            RouteCount = routeCount,
            MqttUsername = reader.MqttUsername,
            MqttPasswordConfigured = !string.IsNullOrWhiteSpace(reader.MqttPasswordHash),
            MqttBrokerReportsOnline = mqttOnline,
            ExampleTagTopic = $"{prefix}/{customerId}/readers/{dev}/tags",
            ExampleHeartbeatTopic = $"{prefix}/{customerId}/readers/{dev}/heartbeat",
            ExampleStatusTopic = $"{prefix}/{customerId}/readers/{dev}/status",
            ExampleCmdTopic = $"{prefix}/{customerId}/readers/{dev}/cmd"
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateMqttCredentials(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var entity = await _dbContext.Readers.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);
        if (entity == null)
            return Json(new { ok = false, message = "Reader not found." });

        var baseUser = string.IsNullOrWhiteSpace(entity.MqttUsername)
            ? MqttReaderCredentialHelper.BuildDefaultMqttUsername(customerId, entity.DeviceIdentifier)
            : entity.MqttUsername.Trim();

        var username = await MqttReaderCredentialHelper.EnsureUniqueMqttUsernameAsync(_dbContext, baseUser, entity.Id, cancellationToken);
        var plain = MqttReaderCredentialHelper.GeneratePlainPassword();
        entity.MqttUsername = username;
        entity.MqttPasswordHash = BCrypt.Net.BCrypt.HashPassword(plain, workFactor: 11);
        entity.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var prefix = _mqttOptions.Value.TopicPrefix.Trim().Trim('/');
        var dev = entity.DeviceIdentifier;

        return Json(new
        {
            ok = true,
            mqttUsername = username,
            mqttPassword = plain,
            topicTags = $"{prefix}/{customerId}/readers/{dev}/tags",
            topicHeartbeat = $"{prefix}/{customerId}/readers/{dev}/heartbeat",
            topicStatus = $"{prefix}/{customerId}/readers/{dev}/status",
            topicCmd = $"{prefix}/{customerId}/readers/{dev}/cmd",
            clientId = dev,
            reminder = "Copy the password now; it cannot be shown again."
        });
    }

    [HttpGet]
    public async Task<IActionResult> Get(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var reader = await _dbContext.Readers
            .AsNoTracking()
            .Where(x => x.Id == id && x.CustomerId == customerId)
            .Select(x => new ReaderFormViewModel
            {
                Id = x.Id,
                ReaderName = x.ReaderName,
                DeviceIdentifier = x.DeviceIdentifier,
                DeviceModel = x.DeviceModel,
                ReaderCategory = x.ReaderCategory,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (reader == null)
            return Json(new { ok = false, message = "Reader not found." });

        return Json(new { ok = true, reader });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReaderFormViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        model = NormalizeForm(model);
        ModelState.Remove(nameof(ReaderFormViewModel.Id));
        TryValidateModel(model);

        if (!ModelState.IsValid)
            return Json(new { ok = false, errors = ModelState.ToDictionary(k => k.Key, v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? []) });

        var dupeIdentifier = await _dbContext.Readers.AnyAsync(x => x.CustomerId == customerId && x.DeviceIdentifier == model.DeviceIdentifier, cancellationToken);
        if (dupeIdentifier)
            return Json(new { ok = false, errors = new Dictionary<string, string[]> { [nameof(ReaderFormViewModel.DeviceIdentifier)] = ["Device identifier already exists."] } });

        var entity = new Reader
        {
            CustomerId = customerId,
            ReaderName = model.ReaderName,
            DeviceIdentifier = model.DeviceIdentifier,
            DeviceModel = model.DeviceModel,
            ReaderCategory = model.ReaderCategory,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Readers.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ReaderFormViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        model = NormalizeForm(model);
        if (model.Id == null || model.Id == 0)
            return Json(new { ok = false, message = "Missing id." });

        ModelState.Remove(nameof(ReaderFormViewModel.Id));
        TryValidateModel(model);

        if (!ModelState.IsValid)
            return Json(new { ok = false, errors = ModelState.ToDictionary(k => k.Key, v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? []) });

        var entity = await _dbContext.Readers.FirstOrDefaultAsync(x => x.Id == model.Id && x.CustomerId == customerId, cancellationToken);
        if (entity == null)
            return Json(new { ok = false, message = "Reader not found." });

        var dupeIdentifier = await _dbContext.Readers.AnyAsync(x => x.CustomerId == customerId && x.Id != entity.Id && x.DeviceIdentifier == model.DeviceIdentifier, cancellationToken);
        if (dupeIdentifier)
            return Json(new { ok = false, errors = new Dictionary<string, string[]> { [nameof(ReaderFormViewModel.DeviceIdentifier)] = ["Device identifier already exists."] } });

        entity.ReaderName = model.ReaderName;
        entity.DeviceIdentifier = model.DeviceIdentifier;
        entity.DeviceModel = model.DeviceModel;
        entity.ReaderCategory = model.ReaderCategory;
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

        var entity = await _dbContext.Readers.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);
        if (entity == null)
            return Json(new { ok = false, message = "Reader not found." });

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        _dbContext.ReaderEvents.Add(new ReaderEvent
        {
            CustomerId = customerId,
            ReaderId = id,
            EventType = "deactivated",
            ChangedBy = User.Identity?.Name,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var entity = await _dbContext.Readers.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);
        if (entity == null)
            return Json(new { ok = false, message = "Reader not found." });

        entity.IsActive = true;
        entity.UpdatedAt = DateTime.UtcNow;
        _dbContext.ReaderEvents.Add(new ReaderEvent
        {
            CustomerId = customerId,
            ReaderId = id,
            EventType = "activated",
            ChangedBy = User.Identity?.Name,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var entity = await _dbContext.Readers.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken);
        if (entity == null)
            return Json(new { ok = false, message = "Reader not found." });

        var hasWays = await _dbContext.ReaderWays.AnyAsync(x => x.CustomerId == customerId && x.ReaderId == id, cancellationToken);
        if (hasWays)
            return Json(new { ok = false, message = "Cannot delete: this reader has scan routes configured." });

        var hasScans = await _dbContext.LinenMovementEvents.AnyAsync(x => x.CustomerId == customerId && x.ReaderId == id, cancellationToken);
        if (hasScans)
            return Json(new { ok = false, message = "Cannot delete: this reader has scan history." });

        _dbContext.Readers.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Json(new { ok = true });
    }

    private static ReaderFormViewModel NormalizeForm(ReaderFormViewModel model)
    {
        model.ReaderName = (model.ReaderName ?? string.Empty).Trim();
        model.DeviceIdentifier = (model.DeviceIdentifier ?? string.Empty).Trim();
        model.DeviceModel = string.IsNullOrWhiteSpace(model.DeviceModel) ? null : model.DeviceModel.Trim();

        model.ReaderCategory = string.IsNullOrWhiteSpace(model.ReaderCategory)
            ? "gate"
            : model.ReaderCategory.Trim().ToLowerInvariant();

        if (model.ReaderCategory.Length > 24)
            model.ReaderCategory = model.ReaderCategory[..24];

        return model;
    }
}
