using System.ComponentModel.DataAnnotations;

namespace LaundryMS.Web.Models.ViewModels;

public class LinenAssignmentBulkUpdateViewModel
{
    [Required]
    public List<ulong> SelectedIds { get; set; } = [];

    public ulong? OwnerCustomerId { get; set; }
    public ulong? AssignedEmployeeId { get; set; }
    public ulong? CurrentLocationId { get; set; }

    [StringLength(24)]
    public string? DefaultAssignmentType { get; set; }

    [StringLength(40)]
    public string? CurrentProcessStatus { get; set; }

    [StringLength(24)]
    public string? PhysicalCondition { get; set; }

    public bool? IsActive { get; set; }
}
