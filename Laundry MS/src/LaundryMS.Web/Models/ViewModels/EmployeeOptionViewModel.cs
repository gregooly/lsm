namespace LaundryMS.Web.Models.ViewModels;

public class EmployeeOptionViewModel
{
    public ulong Id { get; init; }

    /// <summary>External tenant id.</summary>
    public ulong CustomerId { get; init; }

    /// <summary>Local client account id (<c>customers.id</c>) when assigned.</summary>
    public ulong? OwnerCustomerId { get; init; }

    public string EmployeeName { get; init; } = string.Empty;
    public string DisplayLabel { get; init; } = string.Empty;
}
