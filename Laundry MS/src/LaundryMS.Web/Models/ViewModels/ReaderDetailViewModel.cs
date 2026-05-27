namespace LaundryMS.Web.Models.ViewModels;

public class ReaderDetailViewModel
{
    public ulong Id { get; init; }
    public string ReaderName { get; init; } = string.Empty;
    public string DeviceIdentifier { get; init; } = string.Empty;
    public string? DeviceModel { get; init; }
    public string ReaderCategory { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime? InstalledAt { get; init; }
    public DateTime? LastHeartbeatAt { get; init; }
    public string? MaintenanceNote { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public DateTime? LastSeenAt { get; init; }
    public int Scans24h { get; init; }
    public int Scans7d { get; init; }
    public int RouteCount { get; init; }

    /// <summary>MQTT username for EMQX HTTP auth.</summary>
    public string? MqttUsername { get; init; }

    public bool MqttPasswordConfigured { get; init; }

    /// <summary>Last MQTT status (LWT) when subscriber ran.</summary>
    public bool? MqttBrokerReportsOnline { get; init; }

    public string ExampleTagTopic { get; init; } = string.Empty;
    public string ExampleHeartbeatTopic { get; init; } = string.Empty;
    public string ExampleStatusTopic { get; init; } = string.Empty;
    public string ExampleCmdTopic { get; init; } = string.Empty;
}
