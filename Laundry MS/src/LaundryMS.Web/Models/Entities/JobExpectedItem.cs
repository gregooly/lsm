namespace LaundryMS.Web.Models.Entities;

public class JobExpectedItem
{
    public ulong Id { get; set; }
    public ulong? CustomerId { get; set; }
    public ulong LogisticsJobId { get; set; }
    public ulong LinenItemId { get; set; }
    public string ExpectedProcessStatus { get; set; } = string.Empty;
    public bool ReachedExpectedStatus { get; set; }
    public DateTime? ReachedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public LogisticsJob LogisticsJob { get; set; } = null!;
    public LinenItem LinenItem { get; set; } = null!;
}
