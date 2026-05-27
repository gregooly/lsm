using System.ComponentModel.DataAnnotations;

namespace LaundryMS.Web.Models.ViewModels;

public class CustomerFormViewModel
{
    public ulong? Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(150, MinimumLength = 1)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string CustomerType { get; set; } = "other";

    [StringLength(150)]
    public string? PrimaryEmail { get; set; }

    [StringLength(30)]
    public string? PrimaryPhone { get; set; }

    public string? AddressText { get; set; }

    public bool IsActive { get; set; } = true;
}
