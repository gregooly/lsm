namespace LaundryMS.Web.Models.Entities;

public class ReaderEvent
{
    public ulong Id { get; set; }
    public ulong? CustomerId { get; set; }
    public ulong ReaderId { get; set; }
    public string EventType { get; set; } = "note";
    public string? Note { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public Reader Reader { get; set; } = null!;
}
