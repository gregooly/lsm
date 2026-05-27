namespace LaundryMS.Web.Models.Entities;

public class ReaderWay
{
    public ulong Id { get; set; }
    public ulong? CustomerId { get; set; }
    public ulong ReaderId { get; set; }
    public string WayName { get; set; } = string.Empty;
    public string MovementDirection { get; set; } = string.Empty;
    public string BusinessPurposeKey { get; set; } = string.Empty;
    public ulong? FromLocationId { get; set; }
    public ulong? ToLocationId { get; set; }
    public string TargetProcessStatus { get; set; } = string.Empty;

    /// <summary>
    /// Antenna index on multi-port readers (URA8: 1–8). Use <c>0</c> for default/fallback when no antenna is matched.
    /// Unique per <see cref="ReaderId"/>.
    /// </summary>
    public int AntennaIndex { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Reader Reader { get; set; } = null!;
    public Location? FromLocation { get; set; }
    public Location? ToLocation { get; set; }
}
