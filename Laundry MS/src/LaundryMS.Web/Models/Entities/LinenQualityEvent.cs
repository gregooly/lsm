namespace LaundryMS.Web.Models.Entities;

public class LinenQualityEvent
{
    public ulong Id { get; set; }
    public ulong? CustomerId { get; set; }
    public ulong LinenItemId { get; set; }
    public string EventType { get; set; } = "note";
    public string? FromCondition { get; set; }
    public string? ToCondition { get; set; }
    public string? Note { get; set; }
    public string? ReportedBy { get; set; }
    public string? ResolvedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public LinenItem LinenItem { get; set; } = null!;
}
