namespace LaundryMS.Web.Models.Entities;

public class Employee
{
    public ulong Id { get; set; }

    /// <summary>External tenant id (JWT / PulsePoint company scope).</summary>
    public ulong CustomerId { get; set; }

    /// <summary>Local <see cref="Customer.Id"/> of the client account this employee belongs to.</summary>
    public ulong? OwnerCustomerId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;
    public string? EmployeeCode { get; set; }
    public string? SizeProfileText { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Customer? OwnerCustomer { get; set; }
}
