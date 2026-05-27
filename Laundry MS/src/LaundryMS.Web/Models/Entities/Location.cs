namespace LaundryMS.Web.Models.Entities;

public class Location
{
    public ulong Id { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string LocationType { get; set; } = string.Empty;
    /// <summary>External tenant id (JWT / PulsePoint company scope).</summary>
    public ulong? CustomerId { get; set; }
    public string? LocationAddressText { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public decimal? GeoLat { get; set; }
    public decimal? GeoLng { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
