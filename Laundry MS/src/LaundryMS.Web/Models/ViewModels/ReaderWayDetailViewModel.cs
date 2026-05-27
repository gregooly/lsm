namespace LaundryMS.Web.Models.ViewModels;

public class ReaderWayDetailViewModel
{
    public ulong Id { get; init; }
    public string WayName { get; init; } = string.Empty;
    public ulong ReaderId { get; init; }
    public string ReaderName { get; init; } = string.Empty;
    public string MovementDirection { get; init; } = string.Empty;
    public string BusinessPurposeKey { get; init; } = string.Empty;
    public ulong? FromLocationId { get; init; }
    public string? FromLocationName { get; init; }
    public ulong? ToLocationId { get; init; }
    public string? ToLocationName { get; init; }
    public string TargetProcessStatus { get; init; } = string.Empty;

    /// <summary>0 = fallback; 1–8 = URA8 antenna index.</summary>
    public int AntennaIndex { get; init; }

    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public DateTime? LastScanAt { get; init; }
    public int Scans24h { get; init; }
    public int Scans7d { get; init; }
    public int ExceptionScans7d { get; init; }
    public int OpenJobsCount { get; init; }

    public IReadOnlyList<ReaderWayEventRowViewModel> RecentEvents { get; init; } = [];
}

public class ReaderWayEventRowViewModel
{
    public string EventType { get; init; } = string.Empty;
    public string? Note { get; init; }
    public string? ChangedBy { get; init; }
    public DateTime CreatedAt { get; init; }
}
