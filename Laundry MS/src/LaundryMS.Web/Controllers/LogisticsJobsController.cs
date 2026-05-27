using LaundryMS.Web.Data;
using LaundryMS.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Controllers;

public class LogisticsJobsController : TenantScopedController
{
    private readonly LaundryMsDbContext _dbContext;

    public LogisticsJobsController(LaundryMsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(
        string kind = "all",
        string? q = null,
        string? jobStatus = null,
        ulong? customerId = null,
        ulong? driverId = null,
        ulong? readerWayId = null,
        ulong? fromLocationId = null,
        ulong? toLocationId = null,
        bool onlyOpen = false,
        bool onlyOverdue = false,
        bool onlyStalled = false,
        bool onlyDeltaMismatch = false,
        int stalledHours = 24,
        DateTime? createdFrom = null,
        DateTime? createdTo = null,
        string sortBy = "created",
        string sortDir = "desc",
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentCustomerId(out var tenantCustomerId))
            return Forbid();
        customerId = tenantCustomerId;

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 10 or > 150 ? 50 : pageSize;

        var query = new LogisticsJobsQueryViewModel
        {
            Kind = string.IsNullOrWhiteSpace(kind) ? "all" : kind.Trim().ToLowerInvariant(),
            Q = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
            JobStatus = string.IsNullOrWhiteSpace(jobStatus) ? null : jobStatus.Trim().ToLowerInvariant(),
            CustomerId = customerId,
            DriverId = driverId,
            ReaderWayId = readerWayId,
            FromLocationId = fromLocationId,
            ToLocationId = toLocationId,
            OnlyOpen = onlyOpen,
            OnlyOverdue = onlyOverdue,
            OnlyStalled = onlyStalled,
            OnlyDeltaMismatch = onlyDeltaMismatch,
            StalledHours = stalledHours,
            CreatedFrom = createdFrom,
            CreatedTo = createdTo,
            SortBy = string.IsNullOrWhiteSpace(sortBy) ? "created" : sortBy.Trim().ToLowerInvariant(),
            SortDir = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc"
        };

        var (jobs, total) = await LogisticsJobsListLoader.LoadPagedAsync(
            _dbContext,
            query,
            page,
            pageSize,
            cancellationToken);

        var customers = await _dbContext.Customers
            .AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == tenantCustomerId)
            .OrderBy(x => x.CustomerName)
            .Select(x => new CustomerOptionViewModel { Id = x.Id, CustomerName = x.CustomerName })
            .ToListAsync(cancellationToken);

        var drivers = await _dbContext.Drivers
            .AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == tenantCustomerId)
            .OrderBy(x => x.DriverName)
            .Select(x => new DriverOptionViewModel { Id = x.Id, DriverName = x.DriverName })
            .ToListAsync(cancellationToken);

        var locations = await _dbContext.Locations
            .AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == tenantCustomerId)
            .OrderBy(x => x.LocationName)
            .Select(x => new LocationOptionViewModel { Id = x.Id, LocationName = x.LocationName })
            .ToListAsync(cancellationToken);

        var readerWays = await _dbContext.ReaderWays
            .AsNoTracking()
            .Where(x => x.IsActive && x.CustomerId == tenantCustomerId)
            .OrderBy(x => x.WayName)
            .Select(x => new ReaderWayOptionViewModel
            {
                Id = x.Id,
                DisplayName = x.WayName + " - " + x.Reader.ReaderName
            })
            .ToListAsync(cancellationToken);

        var jobStatuses = await _dbContext.LogisticsJobs
            .AsNoTracking()
            .Where(x => x.CustomerId == tenantCustomerId)
            .Select(x => x.JobStatus)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var today = now.Date;
        var stalledCutoff = now.AddHours(-(query.StalledHours is < 1 or > 240 ? 24 : query.StalledHours));

        var openCount = await _dbContext.LogisticsJobs.AsNoTracking()
            .CountAsync(x => x.CustomerId == tenantCustomerId && x.JobStatus == "open", cancellationToken);
        var inProgressCount = await _dbContext.LogisticsJobs.AsNoTracking()
            .CountAsync(x => x.CustomerId == tenantCustomerId && x.JobStatus == "in_progress", cancellationToken);
        var completedTodayCount = await _dbContext.LogisticsJobs.AsNoTracking()
            .CountAsync(x => x.CustomerId == tenantCustomerId && x.JobStatus == "completed" && x.ActualEndAt.HasValue && x.ActualEndAt.Value >= today, cancellationToken);
        var overdueCount = await _dbContext.LogisticsJobs.AsNoTracking()
            .CountAsync(x => x.CustomerId == tenantCustomerId && (x.JobStatus == "open" || x.JobStatus == "in_progress")
                && x.PlannedEndAt.HasValue
                && x.PlannedEndAt.Value < now, cancellationToken);
        var stalledCount = await _dbContext.LogisticsJobs.AsNoTracking()
            .CountAsync(x => x.CustomerId == tenantCustomerId && (x.JobStatus == "open" || x.JobStatus == "in_progress")
                && (!_dbContext.LinenMovementEvents.Any(e => e.CustomerId == tenantCustomerId && e.LogisticsJobId == x.Id)
                    || _dbContext.LinenMovementEvents.Where(e => e.CustomerId == tenantCustomerId && e.LogisticsJobId == x.Id).Max(e => (DateTime?)e.OccurredAt) < stalledCutoff), cancellationToken);

        return View(new LogisticsJobsIndexViewModel
        {
            Jobs = jobs,
            Query = query,
            Customers = customers,
            Drivers = drivers,
            Locations = locations,
            ReaderWays = readerWays,
            JobStatuses = jobStatuses,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            OpenCount = openCount,
            InProgressCount = inProgressCount,
            CompletedTodayCount = completedTodayCount,
            OverdueCount = overdueCount,
            StalledCount = stalledCount
        });
    }

    public async Task<IActionResult> Details(ulong id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var row = await _dbContext.LogisticsJobs.AsNoTracking()
            .Where(x => x.Id == id && x.CustomerId == customerId)
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
                x.ReaderWayId,
                ReaderWayName = x.ReaderWay != null ? x.ReaderWay.WayName : null,
                x.PlannedStartAt,
                x.PlannedEndAt,
                x.ActualStartAt,
                x.ActualEndAt,
                x.Notes,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row == null)
            return NotFound();

        var expectedCount = await _dbContext.JobExpectedItems.AsNoTracking()
            .CountAsync(x => x.CustomerId == customerId && x.LogisticsJobId == id, cancellationToken);
        var reachedCount = await _dbContext.JobExpectedItems.AsNoTracking()
            .CountAsync(x => x.CustomerId == customerId && x.LogisticsJobId == id && x.ReachedExpectedStatus, cancellationToken);

        var scans = await _dbContext.LinenMovementEvents.AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.LogisticsJobId == id)
            .OrderByDescending(x => x.OccurredAt)
            .Select(x => new LogisticsJobScanRowViewModel
            {
                EventId = x.Id,
                RfidTag = x.LinenItem.RfidTag,
                ReaderName = x.Reader.ReaderName,
                ReaderWayName = x.ReaderWay.WayName,
                ProcessingResult = x.ProcessingResult,
                OccurredAt = x.OccurredAt
            })
            .Take(50)
            .ToListAsync(cancellationToken);

        var uniqueTagCount = await _dbContext.LinenMovementEvents.AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.LogisticsJobId == id)
            .Select(x => x.LinenItemId)
            .Distinct()
            .CountAsync(cancellationToken);

        var lastScan = scans.Count == 0 ? (DateTime?)null : scans[0].OccurredAt;

        var model = new LogisticsJobDetailViewModel
        {
            Id = row.Id,
            JobType = row.JobType,
            JobStatus = row.JobStatus,
            CustomerName = row.CustomerName,
            DriverName = row.DriverName,
            FromLocationName = row.FromLocationName,
            ToLocationName = row.ToLocationName,
            ReaderWayId = row.ReaderWayId,
            ReaderWayName = row.ReaderWayName,
            PlannedStartAt = row.PlannedStartAt,
            PlannedEndAt = row.PlannedEndAt,
            ActualStartAt = row.ActualStartAt,
            ActualEndAt = row.ActualEndAt,
            Notes = row.Notes,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt,
            LastScanAt = lastScan,
            ExpectedItemCount = expectedCount,
            ReachedItemCount = reachedCount,
            ScannedUniqueTagCount = uniqueTagCount,
            RecentScans = scans
        };

        return View(model);
    }

}
