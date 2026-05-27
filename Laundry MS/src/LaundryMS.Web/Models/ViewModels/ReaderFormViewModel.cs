using System.ComponentModel.DataAnnotations;

namespace LaundryMS.Web.Models.ViewModels;

public class ReaderFormViewModel
{
    public ulong? Id { get; set; }

    [Required(ErrorMessage = "Reader name is required.")]
    [StringLength(160, MinimumLength = 1)]
    public string ReaderName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Device identifier is required.")]
    [StringLength(120, MinimumLength = 1)]
    public string DeviceIdentifier { get; set; } = string.Empty;

    [StringLength(100)]
    public string? DeviceModel { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    [StringLength(24, MinimumLength = 1)]
    public string ReaderCategory { get; set; } = "gate";

    public bool IsActive { get; set; } = true;
}

