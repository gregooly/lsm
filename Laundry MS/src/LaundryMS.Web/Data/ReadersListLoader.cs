using LaundryMS.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Data;

/// <summary>
/// Loads readers without navigation Count() inside a single SQL projection.
/// </summary>
public static class ReadersListLoader
{
    public static async Task<IReadOnlyList<ReaderListItemViewModel>> LoadAsync(
        LaundryMsDbContext db,
        CancellationToken cancellationToken)
    {
        var readers = await db.Readers
            .AsNoTracking()
            .OrderBy(x => x.ReaderCategory)
            .ThenBy(x => x.ReaderName)
            .Select(x => new ReaderHeadRow
            {
                Id = x.Id,
                ReaderName = x.ReaderName,
                DeviceIdentifier = x.DeviceIdentifier,
                DeviceModel = x.DeviceModel,
                ReaderCategory = x.ReaderCategory,
                IsActive = x.IsActive
            })
            .Take(200)
            .ToListAsync(cancellationToken);

        if (readers.Count == 0)
        {
            return [];
        }

        var readerIds = readers.Select(r => r.Id).ToList();
        var wayCounts = await db.ReaderWays
            .AsNoTracking()
            .Where(w => readerIds.Contains(w.ReaderId))
            .GroupBy(w => w.ReaderId)
            .Select(g => new { ReaderId = g.Key, C = g.Count() })
            .ToDictionaryAsync(x => x.ReaderId, x => x.C, cancellationToken);

        return readers.Select(r => new ReaderListItemViewModel
        {
            Id = r.Id,
            ReaderName = r.ReaderName,
            DeviceIdentifier = r.DeviceIdentifier,
            DeviceModel = r.DeviceModel,
            ReaderCategory = r.ReaderCategory,
            IsActive = r.IsActive,
            WayCount = wayCounts.GetValueOrDefault(r.Id)
        }).ToList();
    }

    private sealed class ReaderHeadRow
    {
        public ulong Id { get; init; }
        public string ReaderName { get; init; } = string.Empty;
        public string DeviceIdentifier { get; init; } = string.Empty;
        public string? DeviceModel { get; init; }
        public string ReaderCategory { get; init; } = string.Empty;
        public bool IsActive { get; init; }
    }
}
