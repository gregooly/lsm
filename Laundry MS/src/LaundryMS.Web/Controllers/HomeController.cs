using System.Diagnostics;
using LaundryMS.Web.Data;
using LaundryMS.Web.Models;
using LaundryMS.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Controllers;

public class HomeController : TenantScopedController
{
    private static readonly (string Key, string Label)[] CanonicalPipelineStatuses =
    [
        ("at_customer", "At customer"),
        ("picked_up", "Picked up"),
        ("arrived_laundry", "Arrived at laundry"),
        ("waiting_cleaning", "Waiting cleaning"),
        ("in_wash", "In wash"),
        ("cleaned", "Cleaned"),
        ("sorting", "Sorting / preparation"),
        ("ready_for_dispatch", "Ready for dispatch"),
        ("dispatched", "Dispatched")
    ];

    private readonly ILogger<HomeController> _logger;
    private readonly LaundryMsDbContext _dbContext;

    public HomeController(ILogger<HomeController> logger, LaundryMsDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Dashboard));
    }

    public async Task<IActionResult> Pipeline(bool showEmptyStages = false, CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        try
        {
            var total = await _dbContext.LinenItems.AsNoTracking()
                .CountAsync(x => x.IsActive && x.CustomerId == customerId, cancellationToken);
            if (total == 0)
            {
                return View(new PipelineViewModel
                {
                    TotalActiveItems = 0,
                    UnassignedLocationCount = 0,
                    NonCanonicalStatusCount = 0,
                    ShowEmptyStages = showEmptyStages,
                    ByProcessStatus = [],
                    ByLocation = []
                });
            }

            var byStatus = await _dbContext.LinenItems.AsNoTracking()
                .Where(x => x.IsActive && x.CustomerId == customerId)
                .GroupBy(x => x.CurrentProcessStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

            var groupedCounts = byStatus
                .GroupBy(x => NormalizeStatus(x.Status))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));

            var statusRows = new List<PipelineStatusRow>();
            foreach (var (key, label) in CanonicalPipelineStatuses)
            {
                var count = groupedCounts.GetValueOrDefault(key);
                statusRows.Add(new PipelineStatusRow
                {
                    StatusKey = key,
                    Status = label,
                    Count = count,
                    Percent = PercentOf(count, total),
                    IsCanonical = true
                });
            }

            var canonicalKeys = CanonicalPipelineStatuses
                .Select(x => x.Key)
                .ToHashSet(StringComparer.Ordinal);

            var nonCanonicalRows = byStatus
                .Select(x => new
                {
                    Key = NormalizeStatus(x.Status),
                    Original = string.IsNullOrWhiteSpace(x.Status) ? "(empty status)" : x.Status.Trim(),
                    x.Count
                })
                .Where(x => !canonicalKeys.Contains(x.Key))
                .OrderByDescending(x => x.Count)
                .Select(x => new PipelineStatusRow
                {
                    StatusKey = x.Key,
                    Status = x.Original,
                    Count = x.Count,
                    Percent = PercentOf(x.Count, total),
                    IsCanonical = false
                });

            statusRows.AddRange(nonCanonicalRows);

            var byLoc = await _dbContext.LinenItems.AsNoTracking()
                .Where(x => x.IsActive && x.CustomerId == customerId)
                .GroupBy(x => x.CurrentLocationId)
                .Select(g => new { LocationId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

            var knownLocationIds = byLoc
                .Where(x => x.LocationId.HasValue)
                .Select(x => x.LocationId!.Value)
                .Distinct()
                .ToList();

            var locationNameMap = await _dbContext.Locations.AsNoTracking()
                .Where(x => x.CustomerId == customerId && knownLocationIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.LocationName, cancellationToken);

            var locRows = byLoc.Select(x => new PipelineLocationRow
            {
                LocationId = x.LocationId,
                Label = !x.LocationId.HasValue
                    ? "(Not set)"
                    : locationNameMap.TryGetValue(x.LocationId.Value, out var n)
                        ? n
                        : $"Unknown id {x.LocationId}",
                Count = x.Count,
                Percent = PercentOf(x.Count, total),
                IsUnassigned = !x.LocationId.HasValue
            }).ToList();

            var unassignedCount = locRows.FirstOrDefault(x => x.IsUnassigned)?.Count ?? 0;
            var nonCanonicalStatusCount = statusRows.Where(x => !x.IsCanonical).Sum(x => x.Count);

            return View(new PipelineViewModel
            {
                TotalActiveItems = total,
                UnassignedLocationCount = unassignedCount,
                NonCanonicalStatusCount = nonCanonicalStatusCount,
                ShowEmptyStages = showEmptyStages,
                ByProcessStatus = statusRows,
                ByLocation = locRows
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pipeline view failed.");
            return View(new PipelineViewModel
            {
                TotalActiveItems = 0,
                UnassignedLocationCount = 0,
                NonCanonicalStatusCount = 0,
                ShowEmptyStages = showEmptyStages,
                ByProcessStatus = [],
                ByLocation = []
            });
        }
    }

    private static string NormalizeStatus(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "(empty)"
            : value.Trim().ToLowerInvariant();
    }

    private static int PercentOf(int count, int total)
    {
        return total <= 0 ? 0 : (int)Math.Round(100.0 * count / total);
    }

    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        try
        {
            var customerCount = await _dbContext.Customers.AsNoTracking().CountAsync(x => x.CustomerId == customerId, cancellationToken);
            var locationCount = await _dbContext.Locations.AsNoTracking().CountAsync(x => x.CustomerId == customerId, cancellationToken);
            var readerCount = await _dbContext.Readers.AsNoTracking().CountAsync(x => x.CustomerId == customerId, cancellationToken);
            var linenItemCount = await _dbContext.LinenItems.AsNoTracking().CountAsync(x => x.CustomerId == customerId, cancellationToken);
            var openJobCount = await _dbContext.LogisticsJobs.AsNoTracking()
                .CountAsync(x => x.CustomerId == customerId && (x.JobStatus == "open" || x.JobStatus == "in_progress"), cancellationToken);
            var pendingExpectedItems = await _dbContext.JobExpectedItems.AsNoTracking()
                .CountAsync(x => x.CustomerId == customerId && !x.ReachedExpectedStatus, cancellationToken);
            var damagedLinenCount = await _dbContext.LinenItems.AsNoTracking()
                .CountAsync(x => x.CustomerId == customerId && x.PhysicalCondition != "good", cancellationToken);

            var since24h = DateTime.UtcNow.AddHours(-24);
            var scansLast24Hours = await _dbContext.LinenMovementEvents.AsNoTracking()
                .CountAsync(x => x.CustomerId == customerId && x.OccurredAt >= since24h, cancellationToken);

            var recentMovements = await _dbContext.LinenMovementEvents
                .AsNoTracking()
                .Where(x => x.CustomerId == customerId)
                .OrderByDescending(x => x.OccurredAt)
                .Select(x => new RecentMovementItemViewModel
                {
                    RfidTag = x.LinenItem.RfidTag,
                    ReaderName = x.Reader.ReaderName,
                    WayName = x.ReaderWay.WayName,
                    OccurredAt = x.OccurredAt,
                    ProcessingResult = x.ProcessingResult
                })
                .Take(8)
                .ToListAsync(cancellationToken);

            var itemsByStatus = await _dbContext.LinenItems.AsNoTracking()
                .Where(x => x.CustomerId == customerId)
                .GroupBy(x => x.CurrentProcessStatus)
                .Select(g => new StatusCountRowViewModel { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

            var locationNameMap = await _dbContext.Locations.AsNoTracking()
                .Where(x => x.CustomerId == customerId)
                .ToDictionaryAsync(x => x.Id, x => x.LocationName, cancellationToken);

            var itemsByLocation = await _dbContext.LinenItems.AsNoTracking()
                .Where(x => x.CustomerId == customerId)
                .GroupBy(x => x.CurrentLocationId)
                .Select(g => new { LocationId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var locationRows = itemsByLocation
                .Select(x => new LocationCountRowViewModel
                {
                    LocationName = x.LocationId.HasValue && locationNameMap.TryGetValue(x.LocationId.Value, out var name)
                        ? name
                        : "Unknown",
                    Count = x.Count
                })
                .OrderByDescending(x => x.Count)
                .Take(12)
                .ToList();

            var topCustomers = await _dbContext.Customers.AsNoTracking()
                .Where(x => x.CustomerId == customerId)
                .OrderBy(x => x.CustomerName)
                .Take(20)
                .Select(c => new { c.Id, c.CustomerName, c.CustomerType })
                .ToListAsync(cancellationToken);

            IReadOnlyList<CustomerOverviewRowViewModel> customerOverview;
            if (topCustomers.Count == 0)
            {
                customerOverview = [];
            }
            else
            {
                var customerIds = topCustomers.Select(c => c.Id).ToList();
                var totalByOwner = await _dbContext.LinenItems.AsNoTracking()
                    .Where(i => i.CustomerId == customerId && i.OwnerCustomerId != null && customerIds.Contains(i.OwnerCustomerId.Value))
                    .GroupBy(i => i.OwnerCustomerId!.Value)
                    .Select(g => new { CustomerId = g.Key, Total = g.Count() })
                    .ToDictionaryAsync(x => x.CustomerId, x => x.Total, cancellationToken);

                var activeByOwner = await _dbContext.LinenItems.AsNoTracking()
                    .Where(i =>
                        i.CustomerId == customerId
                        &&
                        i.OwnerCustomerId != null
                        && customerIds.Contains(i.OwnerCustomerId.Value)
                        && i.IsActive)
                    .GroupBy(i => i.OwnerCustomerId!.Value)
                    .Select(g => new { CustomerId = g.Key, Active = g.Count() })
                    .ToDictionaryAsync(x => x.CustomerId, x => x.Active, cancellationToken);

                customerOverview = topCustomers.Select(c => new CustomerOverviewRowViewModel
                {
                    CustomerName = c.CustomerName,
                    LocationCount = 0,
                    LinenCount = totalByOwner.GetValueOrDefault(c.Id),
                    ActiveLinenCount = activeByOwner.GetValueOrDefault(c.Id),
                    ContractHint = c.CustomerType
                }).ToList();
            }

            var viewModel = new DashboardViewModel
            {
                DatabaseReachable = true,
                CustomerCount = customerCount,
                LocationCount = locationCount,
                ReaderCount = readerCount,
                LinenItemCount = linenItemCount,
                OpenJobCount = openJobCount,
                PendingExpectedItems = pendingExpectedItems,
                DamagedLinenCount = damagedLinenCount,
                ScansLast24Hours = scansLast24Hours,
                RecentMovements = recentMovements,
                ItemsByStatus = itemsByStatus,
                ItemsByLocation = locationRows,
                CustomerOverview = customerOverview
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dashboard data loading failed.");
            var viewModel = new DashboardViewModel
            {
                DatabaseReachable = false,
                ErrorMessage = ex.Message
            };
            return View(viewModel);
        }
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
