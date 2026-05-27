namespace LaundryMS.Web.Models.Entities;

public class Reader
{
    public ulong Id { get; set; }
    public ulong? CustomerId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string? DeviceModel { get; set; }
    public string ReaderCategory { get; set; } = string.Empty;
    public DateTime? InstalledAt { get; set; }
    public DateTime? LastHeartbeatAt { get; set; }
    public string? MaintenanceNote { get; set; }

    /// <summary>MQTT username for EMQX HTTP auth (Strategy B); unique across all tenants.</summary>
    public string? MqttUsername { get; set; }

    /// <summary>BCrypt hash of MQTT password for fixed readers (URA8).</summary>
    public string? MqttPasswordHash { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ReaderWay> ReaderWays { get; set; } = new List<ReaderWay>();
}
