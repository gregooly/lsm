using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LaundryMS.Web.Models.Api;

public class TableLoginRequest
{
    public string? DeviceId { get; set; }

    [JsonPropertyName("handheldId")]
    public string? HandheldId { get; set; }

    public string? HandheldDeviceId { get; set; }
}

/// <summary>Bluetooth connection to the Chainway table reader (handheld id only; driver comes from JWT).</summary>
public class TableConnectionRequest
{
    /// <summary>Chainway reader device identifier from Bluetooth (maps to readers.device_identifier).</summary>
    [Required]
    [JsonPropertyName("handheldId")]
    public string HandheldId { get; set; } = string.Empty;

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    public ulong CustomerId { get; set; }

    public DateTime ConnectedAt { get; set; }
}

public class TableLinenMovementBatchRequest
{
    public ulong CustomerId { get; set; }

    public ulong ReaderId { get; set; }

    public ulong ReaderWayId { get; set; }

    public DateTime UploadedAt { get; set; }

    [Required]
    [MinLength(1)]
    public List<TableMovementEventItemDto> Events { get; set; } = [];
}

public class TableMovementEventItemDto
{
    [Required]
    public string IdempotencyKey { get; set; } = string.Empty;

    [Required]
    public string RfidTag { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }

    /// <summary>Required for table ingest: good, damaged, or lost.</summary>
    [Required]
    public string ConditionAfterEvent { get; set; } = string.Empty;
}

/// <summary>Returned from table-login before a Bluetooth reader is verified.</summary>
public class TableLoginBootstrapData
{
    public List<DriverLocationApiDto> Locations { get; init; } = [];

    public DriverSyncPolicyApiDto SyncPolicy { get; init; } = new();
}

public class TableBootstrapData
{
    public TableReaderApiDto Reader { get; init; } = new();

    public List<ReaderWayApiDto> ReaderWays { get; init; } = [];

    public List<DriverLocationApiDto> Locations { get; init; } = [];

    public DriverSyncPolicyApiDto SyncPolicy { get; init; } = new();
}

public class TableReaderApiDto
{
    public ulong Id { get; set; }

    public string ReaderName { get; set; } = string.Empty;

    public string DeviceIdentifier { get; set; } = string.Empty;

    public string? DeviceModel { get; set; }

    public string ReaderCategory { get; set; } = string.Empty;
}

public class TableLinenLookupResponse
{
    public bool Found { get; set; }

    public TableLinenItemApiDto? Item { get; set; }

    public List<string> Warnings { get; set; } = [];
}

public class TableLinenItemApiDto
{
    public ulong Id { get; set; }

    public string RfidTag { get; set; } = string.Empty;

    public string ItemType { get; set; } = string.Empty;

    public string? SizeLabel { get; set; }

    public string DefaultAssignmentType { get; set; } = string.Empty;

    public ulong? OwnerCustomerId { get; set; }

    public string? OwnerCustomerName { get; set; }

    public ulong? AssignedEmployeeId { get; set; }

    public string? AssignedEmployeeName { get; set; }

    public ulong? CurrentLocationId { get; set; }

    public string? CurrentLocationName { get; set; }

    public string CurrentProcessStatus { get; set; } = string.Empty;

    public string PhysicalCondition { get; set; } = string.Empty;

    public DateTime? LastScannedAt { get; set; }

    public bool IsActive { get; set; }
}
