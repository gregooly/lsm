using System.ComponentModel.DataAnnotations;

namespace LaundryMS.Web.Models.ViewModels;

public class LinenItemFormViewModel
{
    public ulong? Id { get; set; }

    [Required(ErrorMessage = "RFID tag is required.")]
    [StringLength(120, MinimumLength = 1)]
    public string RfidTag { get; set; } = string.Empty;

    [Required(ErrorMessage = "Item type is required.")]
    [StringLength(100, MinimumLength = 1)]
    public string ItemType { get; set; } = string.Empty;

    [StringLength(20)]
    public string? SizeLabel { get; set; }

    [Required]
    [StringLength(24)]
    public string DefaultAssignmentType { get; set; } = "fixed";

    public ulong? OwnerCustomerId { get; set; }

    public ulong? AssignedEmployeeId { get; set; }

    public ulong? CurrentLocationId { get; set; }

    [Required]
    [StringLength(40)]
    public string CurrentProcessStatus { get; set; } = "at_customer";

    [Required]
    [StringLength(24)]
    public string PhysicalCondition { get; set; } = "good";

    public bool IsActive { get; set; } = true;
}

