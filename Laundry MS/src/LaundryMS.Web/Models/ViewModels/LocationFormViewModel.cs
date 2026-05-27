using System.ComponentModel.DataAnnotations;

namespace LaundryMS.Web.Models.ViewModels;

public class LocationFormViewModel
{
    public ulong? Id { get; set; }

    [Required(ErrorMessage = "Location name is required.")]
    [StringLength(150, MinimumLength = 1)]
    public string LocationName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Location type is required.")]
    [StringLength(32, MinimumLength = 1)]
    public string LocationType { get; set; } = string.Empty;

    public ulong? CustomerId { get; set; }

    [StringLength(500)]
    public string? LocationAddressText { get; set; }

    [StringLength(120)]
    public string? ContactPerson { get; set; }

    [StringLength(30)]
    public string? ContactPhone { get; set; }

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal? GeoLat { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal? GeoLng { get; set; }

    public bool IsActive { get; set; } = true;
}

