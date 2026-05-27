namespace LaundryMS.Web.Options;

public sealed class MqttOptions
{
    public const string SectionName = "Mqtt";

    /// <summary>When false, the MQTT subscriber hosted service does not start.</summary>
    public bool Enabled { get; set; }

    public string BrokerHost { get; set; } = string.Empty;

    public int BrokerPort { get; set; } = 8883;

    public bool UseTls { get; set; } = true;

    /// <summary>LaundryMS subscriber client id (must be unique on the broker).</summary>
    public string SubscriberClientId { get; set; } = "laundryms-subscriber";

    public string SubscriberUsername { get; set; } = string.Empty;

    public string SubscriberPassword { get; set; } = string.Empty;

    /// <summary>Topic prefix (first segment). Default matches deployment docs.</summary>
    public string TopicPrefix { get; set; } = "laundryms";

    /// <summary>Shared secret EMQX sends as header when calling auth/ACL webhooks.</summary>
    public string WebhookSharedSecret { get; set; } = string.Empty;

    public int IdempotencyWindowMs { get; set; } = 1000;

    /// <summary>Minimum interval between persisting heartbeat rows to reader_events.</summary>
    public int HeartbeatDbLogMinSeconds { get; set; } = 3600;
}
