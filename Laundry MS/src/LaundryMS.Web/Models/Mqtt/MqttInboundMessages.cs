using System.Text.Json.Serialization;

namespace LaundryMS.Web.Models.Mqtt;

public sealed class MqttTagBatchMessage
{
    public string? BatchId { get; set; }

    public string? DeviceIdentifier { get; set; }

    public DateTime? SentAt { get; set; }

    public List<MqttTagEventDto>? Events { get; set; }
}

public sealed class MqttTagEventDto
{
    public string? Epc { get; set; }

    public string? Tid { get; set; }

    public int? Antenna { get; set; }

    public int? Rssi { get; set; }

    public DateTime OccurredAt { get; set; }
}

public sealed class MqttHeartbeatMessage
{
    public string? DeviceIdentifier { get; set; }

    public DateTime? SentAt { get; set; }

    [JsonPropertyName("firmware")]
    public string? Firmware { get; set; }

    [JsonPropertyName("uptimeSec")]
    public long? UptimeSec { get; set; }

    [JsonPropertyName("moduleTempC")]
    public int? ModuleTempC { get; set; }

    public string? Ip { get; set; }

    [JsonPropertyName("rssiAvg")]
    public int? RssiAvg { get; set; }

    [JsonPropertyName("tagsInLastMinute")]
    public int? TagsInLastMinute { get; set; }
}

public sealed class MqttStatusMessage
{
    public string? State { get; set; }

    public DateTime? Ts { get; set; }
}
