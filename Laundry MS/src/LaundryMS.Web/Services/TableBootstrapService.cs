using LaundryMS.Web.Data;
using LaundryMS.Web.Models.Api;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Services;

public sealed class TableBootstrapService : ITableBootstrapService
{
    private readonly LaundryMsDbContext _db;

    public TableBootstrapService(LaundryMsDbContext db)
    {
        _db = db;
    }

    public async Task<TableLoginBootstrapData> GetLoginBootstrapAsync(ulong customerId, CancellationToken cancellationToken)
    {
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

        return new TableLoginBootstrapData
        {
            Locations = locations,
            SyncPolicy = new DriverSyncPolicyApiDto(),
        };
    }

    public async Task<TableBootstrapData?> GetAsync(ulong customerId, ulong readerId, CancellationToken cancellationToken)
    {
        var reader = await _db.Readers.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == readerId && x.CustomerId == customerId && x.IsActive,
                cancellationToken)
            .ConfigureAwait(false);

        if (reader == null)
            return null;

        var readerWays = await _db.ReaderWays.AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.ReaderId == readerId && x.IsActive)
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
                ReaderName = reader.ReaderName,
                DeviceIdentifier = reader.DeviceIdentifier,
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

        return new TableBootstrapData
        {
            Reader = new TableReaderApiDto
            {
                Id = reader.Id,
                ReaderName = reader.ReaderName,
                DeviceIdentifier = reader.DeviceIdentifier,
                DeviceModel = reader.DeviceModel,
                ReaderCategory = reader.ReaderCategory,
            },
            ReaderWays = readerWays,
            Locations = locations,
            SyncPolicy = new DriverSyncPolicyApiDto(),
        };
    }
}
