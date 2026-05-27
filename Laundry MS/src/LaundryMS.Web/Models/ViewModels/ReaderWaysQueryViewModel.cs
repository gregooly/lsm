namespace LaundryMS.Web.Models.ViewModels;

public class ReaderWaysQueryViewModel
{
    public string? Q { get; init; }
    public ulong? ReaderId { get; init; }
    public ulong? FromLocationId { get; init; }
    public ulong? ToLocationId { get; init; }
    public string? BusinessPurposeKey { get; init; }
    public string? MovementDirection { get; init; }
    public string? TargetProcessStatus { get; init; }
    public bool IncludeInactive { get; init; }
    public bool OnlySilent { get; init; }
    public bool OnlyMissingEndpoints { get; init; }
    public string SortBy { get; init; } = "name";
    public string SortDir { get; init; } = "asc";
}
