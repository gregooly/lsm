namespace LaundryMS.Web.Services;

/// <summary>MQTT-visible presence / heartbeat hints keyed by tenant device identifier.</summary>
public interface IMqttReaderStateRegistry
{
    void SetConnectionState(ulong customerId, string deviceIdentifier, bool online);

    bool TryGetConnectionState(ulong customerId, string deviceIdentifier, out bool online);

    bool ShouldPersistHeartbeat(ulong readerId, TimeSpan minInterval);
}

public sealed class MqttReaderStateRegistry : IMqttReaderStateRegistry
{
    private readonly object _lock = new();
    private readonly Dictionary<string, bool> _onlineByDevice = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ulong, DateTime> _lastHeartbeatPersistUtcByReader = [];

    private static string Key(ulong customerId, string deviceIdentifier)
        => $"{customerId}|{(deviceIdentifier ?? string.Empty).Trim()}";

    public void SetConnectionState(ulong customerId, string deviceIdentifier, bool online)
    {
        lock (_lock)
        {
            _onlineByDevice[Key(customerId, deviceIdentifier)] = online;
        }
    }

    public bool TryGetConnectionState(ulong customerId, string deviceIdentifier, out bool online)
    {
        lock (_lock)
        {
            return _onlineByDevice.TryGetValue(Key(customerId, deviceIdentifier), out online);
        }
    }

    public bool ShouldPersistHeartbeat(ulong readerId, TimeSpan minInterval)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            if (_lastHeartbeatPersistUtcByReader.TryGetValue(readerId, out var last))
            {
                if (now - last < minInterval)
                    return false;
            }

            _lastHeartbeatPersistUtcByReader[readerId] = now;
            return true;
        }
    }
}
