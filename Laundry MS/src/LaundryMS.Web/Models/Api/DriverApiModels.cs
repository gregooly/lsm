using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LaundryMS.Web.Models.Api;

public class DeviceLoginRequest
{
    /// <summary>Primary field name used by the backend.</summary>
    public string? HandheldDeviceId { get; set; }

    /// <summary>Alias for Android clients.</summary>
    public string? DeviceId { get; set; }

    /// <summary>Android clients often send this key for the driver phone / device id at login.</summary>
    [JsonPropertyName("handheldId")]
    public string? HandheldId { get; set; }
}

public class R2ConnectionRequest
{
    /// <summary>Android sends JSON field <c>handheldId</c> (R2 reader device identifier).</summary>
    [Required]
    [JsonPropertyName("handheldId")]
    public string HandheldId { get; set; } = string.Empty;

    public ulong DriverId { get; set; }

    public ulong CustomerId { get; set; }

    public DateTime ConnectedAt { get; set; }

    public string ConnectionType { get; set; } = "bluetooth";
}

public class LinenMovementBatchRequest
{
    public string? HandheldDeviceId { get; set; }

    public ulong DriverId { get; set; }

    public ulong CustomerId { get; set; }

    public ulong? JobId { get; set; }

    public ulong ReaderId { get; set; }

    public ulong ReaderWayId { get; set; }

    public DateTime UploadedAt { get; set; }

    [Required]
    [MinLength(1)]
    public List<MovementEventItemDto> Events { get; set; } = [];
}

public class MovementEventItemDto
{
    [Required]
    public string IdempotencyKey { get; set; } = string.Empty;

    [Required]
    public string RfidTag { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }

    public ulong? JobId { get; set; }

    public ulong? ReaderId { get; set; }

    public ulong? ReaderWayId { get; set; }

    public ulong? DriverId { get; set; }

    public string? ConditionAfterEvent { get; set; }
}

/// <summary>Payload for driver app after login or GET bootstrap.</summary>
public class DriverBootstrapData
{
    public List<ActiveTaskApiDto> ActiveTasks { get; init; } = [];

    public List<DriverLocationApiDto> Locations { get; init; } = [];

    public List<ReaderWayApiDto> ReaderWays { get; init; } = [];

    public DriverSyncPolicyApiDto SyncPolicy { get; init; } = new();
}

public class ActiveTaskApiDto
{
    public ulong Id { get; set; }

    public string JobType { get; set; } = string.Empty;

    public string JobStatus { get; set; } = string.Empty;

    public ulong? ReaderWayId { get; set; }

    public ulong? ReaderId { get; set; }

    public string? WayName { get; set; }

    public string? TargetProcessStatus { get; set; }

    public string? MovementDirection { get; set; }

    public LocationRefApiDto? FromLocation { get; set; }

    public LocationRefApiDto? ToLocation { get; set; }

    public DateTime? PlannedStartAt { get; set; }

    public DateTime? PlannedEndAt { get; set; }

    public int PendingExpectedItemsCount { get; set; }

    public int TotalExpectedItemsCount { get; set; }
}

public class LocationRefApiDto
{
    public ulong Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class DriverLocationApiDto
{
    public ulong Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Type { get; set; }

    public string? Address { get; set; }

    public decimal? GeoLat { get; set; }

    public decimal? GeoLng { get; set; }

    public bool IsActive { get; set; }
}

public class ReaderWayApiDto
{
    public ulong Id { get; set; }

    public ulong ReaderId { get; set; }

    public string WayName { get; set; } = string.Empty;

    public string MovementDirection { get; set; } = string.Empty;

    public string BusinessPurposeKey { get; set; } = string.Empty;

    public string TargetProcessStatus { get; set; } = string.Empty;

    public ulong? FromLocationId { get; set; }

    public ulong? ToLocationId { get; set; }

    public string ReaderName { get; set; } = string.Empty;

    public string DeviceIdentifier { get; set; } = string.Empty;
}

public class DriverSyncPolicyApiDto
{
    public int MaxBatchSize { get; set; } = 200;

    public int RecommendedSyncIntervalSeconds { get; set; } = 30;

    public int MaxRetrySeconds { get; set; } = 120;
}
