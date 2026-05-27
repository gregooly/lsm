using LaundryMS.Web.Data;
using LaundryMS.Web.Models.Api;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Services;

public class DriverBootstrapService : IDriverBootstrapService
{
    private readonly LaundryMsDbContext _db;

    public DriverBootstrapService(LaundryMsDbContext db)
    {
        _db = db;
    }

    public async Task<DriverBootstrapData> GetAsync(ulong customerId, ulong driverId, CancellationToken cancellationToken)
    {
        var activeTasks = await _db.LogisticsJobs.AsNoTracking()
            .Where(j => j.CustomerId == customerId && j.DriverId == driverId)
            .Where(j => j.JobStatus == "open" || j.JobStatus == "in_progress")
            .OrderBy(j => j.PlannedStartAt ?? j.CreatedAt)
            .Select(j => new ActiveTaskApiDto
            {
                Id = j.Id,
                JobType = j.JobType,
                JobStatus = j.JobStatus,
                ReaderWayId = j.ReaderWayId,
                ReaderId = j.ReaderWay != null ? j.ReaderWay.ReaderId : null,
                WayName = j.ReaderWay != null ? j.ReaderWay.WayName : null,
                TargetProcessStatus = j.ReaderWay != null ? j.ReaderWay.TargetProcessStatus : null,
                MovementDirection = j.ReaderWay != null ? j.ReaderWay.MovementDirection : null,
                FromLocation = j.FromLocation == null
                    ? null
                    : new LocationRefApiDto { Id = j.FromLocation.Id, Name = j.FromLocation.LocationName },
                ToLocation = j.ToLocation == null
                    ? null
                    : new LocationRefApiDto { Id = j.ToLocation.Id, Name = j.ToLocation.LocationName },
                PlannedStartAt = j.PlannedStartAt,
                PlannedEndAt = j.PlannedEndAt,
                PendingExpectedItemsCount = j.ExpectedItems.Count(e => !e.ReachedExpectedStatus),
                TotalExpectedItemsCount = j.ExpectedItems.Count,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var locations = await _db.Locations.AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.IsActive)
            .OrderBy(x => x.LocationName)
            .Select(x => new DriverLocationApiDto
            {
                Id = x.Id,
                Name = x.LocationName,
                Type = x.LocationType,
                Address = x.LocationAddressText,
                GeoLat = x.GeoLat,
                GeoLng = x.GeoLng,
                IsActive = x.IsActive,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var readerWays = await _db.ReaderWays.AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.IsActive)
            .OrderBy(x => x.WayName)
            .Select(x => new ReaderWayApiDto
            {
                Id = x.Id,
                ReaderId = x.ReaderId,
                WayName = x.WayName,
                MovementDirection = x.MovementDirection,
                BusinessPurposeKey = x.BusinessPurposeKey,
                TargetProcessStatus = x.TargetProcessStatus,
                FromLocationId = x.FromLocationId,
                ToLocationId = x.ToLocationId,
                ReaderName = x.Reader.ReaderName,
                DeviceIdentifier = x.Reader.DeviceIdentifier,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new DriverBootstrapData
        {
            ActiveTasks = activeTasks,
            Locations = locations,
            ReaderWays = readerWays,
            SyncPolicy = new DriverSyncPolicyApiDto(),
        };
    }
}
