using System.ComponentModel.DataAnnotations;

namespace LaundryMS.Web.Models.ViewModels;

public class DriverFormViewModel
{
    public ulong? Id { get; set; }

    [Required(ErrorMessage = "Driver name is required.")]
    [StringLength(150, MinimumLength = 1)]
    public string DriverName { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Phone")]
    public string? MobilePhone { get; set; }

    [StringLength(30)]
    [Display(Name = "Vehicle registration")]
    public string? VehicleRegistrationNo { get; set; }

    [StringLength(120)]
    [Display(Name = "Handheld device ID (R2 / app)")]
    public string? HandheldDeviceId { get; set; }

    public bool IsActive { get; set; } = true;
}

