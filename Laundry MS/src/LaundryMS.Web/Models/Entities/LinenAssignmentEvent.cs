namespace LaundryMS.Web.Models.Entities;

public class LinenAssignmentEvent
{
    public ulong Id { get; set; }
    public ulong? CustomerId { get; set; }
    public ulong LinenItemId { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? ChangedBy { get; set; }
    public string ChangeSource { get; set; } = "manual_ui_edit";
    public string? FromJson { get; set; }
    public string? ToJson { get; set; }
    public string? Note { get; set; }

    public LinenItem LinenItem { get; set; } = null!;
}
