namespace LaundryMS.Web.Models.Entities;

public class LinenItem
{
    public ulong Id { get; set; }
    public ulong? CustomerId { get; set; }
    public string RfidTag { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string? SizeLabel { get; set; }
    public string DefaultAssignmentType { get; set; } = "fixed";
    public ulong? OwnerCustomerId { get; set; }
    public ulong? AssignedEmployeeId { get; set; }
    public ulong? CurrentLocationId { get; set; }
    public string CurrentProcessStatus { get; set; } = "at_customer";
    public string PhysicalCondition { get; set; } = "good";
    public DateTime? LastScannedAt { get; set; }
    public string LifecycleState { get; set; } = "active";
    public string? DeactivationReason { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Customer? OwnerCustomer { get; set; }
    public Employee? AssignedEmployee { get; set; }
    public Location? CurrentLocation { get; set; }
}
