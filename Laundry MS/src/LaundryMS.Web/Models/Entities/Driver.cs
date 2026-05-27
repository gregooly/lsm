namespace LaundryMS.Web.Models.Entities;

public class Driver
{
    public ulong Id { get; set; }
    public ulong? CustomerId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string? MobilePhone { get; set; }
    public string? VehicleRegistrationNo { get; set; }
    /// <summary>Chainway R2 / app-reported device id string (mobile sign-up).</summary>
    public string? HandheldDeviceId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
