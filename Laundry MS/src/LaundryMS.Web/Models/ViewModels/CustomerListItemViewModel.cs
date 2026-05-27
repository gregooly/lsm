namespace LaundryMS.Web.Models.ViewModels;

public class CustomerListItemViewModel
{
    public ulong Id { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerType { get; init; } = string.Empty;

    /// <summary>Human-readable type for the table (never blank).</summary>
    public string CustomerTypeDisplay { get; init; } = string.Empty;
    public string? PrimaryEmail { get; init; }
    public string? PrimaryPhone { get; init; }
    public bool IsActive { get; init; }

    public int EmployeeCount { get; init; }
    public int OwnedLinenActiveCount { get; init; }
    public int OwnedLinenTotalCount { get; init; }
    public int OpenLogisticsJobsCount { get; init; }
    public int DamagedLinenCount { get; init; }
}
