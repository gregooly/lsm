namespace LaundryMS.Web.Models.Entities;

public class ReaderWayEvent
{
    public ulong Id { get; set; }
    public ulong? CustomerId { get; set; }
    public ulong ReaderWayId { get; set; }
    public string EventType { get; set; } = "note";
    public string? Note { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public ReaderWay ReaderWay { get; set; } = null!;
}
