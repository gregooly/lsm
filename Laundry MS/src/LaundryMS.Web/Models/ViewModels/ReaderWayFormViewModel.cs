using System.ComponentModel.DataAnnotations;

namespace LaundryMS.Web.Models.ViewModels;

public class ReaderWayFormViewModel
{
    public ulong? Id { get; set; }

    [Required]
    public ulong ReaderId { get; set; }

    [Required(ErrorMessage = "Way name is required.")]
    [StringLength(180, MinimumLength = 1)]
    public string WayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Movement direction is required.")]
    [StringLength(16, MinimumLength = 1)]
    public string MovementDirection { get; set; } = "in";

    [Required(ErrorMessage = "Purpose is required.")]
    [StringLength(60, MinimumLength = 1)]
    public string BusinessPurposeKey { get; set; } = "scan";

    public ulong? FromLocationId { get; set; }
    public ulong? ToLocationId { get; set; }

    [Required(ErrorMessage = "Target status is required.")]
    [StringLength(40, MinimumLength = 1)]
    public string TargetProcessStatus { get; set; } = "at_customer";

    /// <summary>0 = fallback/default way; 1–8 = URA8 antenna port mapped to this route.</summary>
    [Range(0, 16, ErrorMessage = "Antenna index must be 0–16.")]
    public int AntennaIndex { get; set; }

    public bool IsActive { get; set; } = true;
}

