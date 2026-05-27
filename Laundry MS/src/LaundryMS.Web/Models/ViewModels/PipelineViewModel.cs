namespace LaundryMS.Web.Models.ViewModels;

public class PipelineViewModel
{
    public int TotalActiveItems { get; init; }
    public int UnassignedLocationCount { get; init; }
    public int NonCanonicalStatusCount { get; init; }
    public bool ShowEmptyStages { get; init; }
    public IReadOnlyList<PipelineStatusRow> ByProcessStatus { get; init; } = [];
    public IReadOnlyList<PipelineLocationRow> ByLocation { get; init; } = [];
}

public class PipelineStatusRow
{
    public string StatusKey { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
    public int Percent { get; init; }
    public bool IsCanonical { get; init; }
}

public class PipelineLocationRow
{
    public ulong? LocationId { get; init; }
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
    public int Percent { get; init; }
    public bool IsUnassigned { get; init; }
}
