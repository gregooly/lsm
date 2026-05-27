namespace LaundryMS.Web.Models.ViewModels;

public class CustomerDetailViewModel
{
    public ulong Id { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerType { get; init; } = string.Empty;
    public string CustomerTypeDisplay { get; init; } = string.Empty;
    public string? PrimaryEmail { get; init; }
    public string? PrimaryPhone { get; init; }
    public string? AddressText { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public int EmployeeCount { get; init; }
    public int OwnedLinenActiveCount { get; init; }
    public int OwnedLinenTotalCount { get; init; }
    public int OpenLogisticsJobsCount { get; init; }
    public int DamagedLinenCount { get; init; }
}
