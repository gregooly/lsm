namespace LaundryMS.Web.Models.ViewModels;

public class QualityIndexViewModel
{
    public IReadOnlyList<QualityItemRowViewModel> Items { get; init; } = [];
    public IReadOnlyList<CustomerOptionViewModel> Customers { get; init; } = [];
    public IReadOnlyList<LocationOptionViewModel> Locations { get; init; } = [];
    public IReadOnlyList<string> ProcessStatuses { get; init; } = [];
    public IReadOnlyList<string> Conditions { get; init; } = [];
    public QualityQueryViewModel Query { get; init; } = new();
    public int OpenIssuesCount { get; init; }
    public int DamagedCount { get; init; }
    public int LostCount { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public int TotalCount { get; init; }

    public int TotalPages =>
        TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class QualityItemRowViewModel
{
    public ulong Id { get; init; }
    public string RfidTag { get; init; } = string.Empty;
    public string ItemType { get; init; } = string.Empty;
    public string PhysicalCondition { get; init; } = string.Empty;
    public string CurrentProcessStatus { get; init; } = string.Empty;
    public string? OwnerCustomerName { get; init; }
    public string? CurrentLocationName { get; init; }
    public DateTime? LastScannedAt { get; init; }
    public int? DaysSinceLastScan { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string LifecycleState { get; init; } = "active";
}

public class QualityQueryViewModel
{
    public string? Q { get; init; }
    public ulong? CustomerId { get; init; }
    public ulong? LocationId { get; init; }
    public string? Condition { get; init; }
    public string? ProcessStatus { get; init; }
    public int? StaleDays { get; init; }
    public bool IncludeInactive { get; init; }
    public string SortBy { get; init; } = "rfid";
    public string SortDir { get; init; } = "asc";
}

public class QualityActionViewModel
{
    public ulong LinenItemId { get; set; }
    public string? NewCondition { get; set; }
    public string? EventType { get; set; }
    public string? Note { get; set; }
}
