namespace LaundryMS.Web.Models.ViewModels;

public class ReportsQueryViewModel
{
    public ulong? CustomerId { get; init; }
    public ulong? ReaderId { get; init; }
    public ulong? ReaderWayId { get; init; }
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public bool OnlyExceptions { get; init; }
}
