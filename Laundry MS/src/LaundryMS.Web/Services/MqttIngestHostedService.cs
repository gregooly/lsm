using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LaundryMS.Web.Data;
using LaundryMS.Web.Models.Entities;
using LaundryMS.Web.Models.Mqtt;
using LaundryMS.Web.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;

namespace LaundryMS.Web.Services;

public sealed class MqttIngestHostedService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MqttIngestHostedService> _logger;
    private readonly MqttOptions _options;

    public MqttIngestHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<MqttOptions> options,
        ILogger<MqttIngestHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BrokerHost))
        {
            _logger.LogInformation("MQTT ingest disabled (Mqtt:Enabled=false or broker host empty).");
            await base.StartAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BrokerHost))
            return;

        var mqttFactory = new MqttFactory();
        using var client = mqttFactory.CreateMqttClient();

        client.ApplicationMessageReceivedAsync += async args =>
        {
            try
            {
                await HandleMessageAsync(args.ApplicationMessage, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MQTT message handler failed.");
            }
        };

        var prefix = _options.TopicPrefix.Trim().Trim('/');
        var subscribeTopic = $"{prefix}/+/readers/+/+";

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var builder = new MqttClientOptionsBuilder()
                    .WithProtocolVersion(MqttProtocolVersion.V311)
                    .WithTcpServer(_options.BrokerHost, _options.BrokerPort)
                    .WithClientId(_options.SubscriberClientId.Trim());

                if (!string.IsNullOrWhiteSpace(_options.SubscriberUsername))
                {
                    builder.WithCredentials(_options.SubscriberUsername.Trim(), _options.SubscriberPassword ?? string.Empty);
                }

                if (_options.UseTls)
                {
                    builder.WithTlsOptions(o => o.WithCertificateValidationHandler(_ => true));
                }

                var opts = builder.Build();

                await client.ConnectAsync(opts, stoppingToken).ConfigureAwait(false);

                var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(t => t.WithTopic(subscribeTopic).WithAtLeastOnceQoS())
                    .Build();

                await client.SubscribeAsync(mqttSubscribeOptions, stoppingToken).ConfigureAwait(false);

                _logger.LogInformation("MQTT subscriber connected to {Host}:{Port}, subscribed {Topic}.", _options.BrokerHost, _options.BrokerPort, subscribeTopic);

                while (client.IsConnected && !stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning("MQTT client disconnected; reconnecting…");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MQTT connect cycle failed; retry in 10s.");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        try
        {
            if (client.IsConnected)
                await client.DisconnectAsync(cancellationToken: stoppingToken).ConfigureAwait(false);
        }
        catch
        {
            /* ignore */
        }
    }

    private async Task HandleMessageAsync(MqttApplicationMessage message, CancellationToken ct)
    {
        var topic = message.Topic ?? string.Empty;
        if (!MqttTopicPaths.TryParseReaderTopic(_options.TopicPrefix, topic, out var customerId, out var deviceIdentifier, out var suffix))
            return;

        var payload = GetPayloadString(message);
        if (string.IsNullOrWhiteSpace(payload))
            return;

        switch (suffix.ToLowerInvariant())
        {
            case "tags":
                await HandleTagsAsync(customerId, deviceIdentifier, payload, ct).ConfigureAwait(false);
                break;
            case "heartbeat":
                await HandleHeartbeatAsync(customerId, deviceIdentifier, payload, ct).ConfigureAwait(false);
                break;
            case "status":
                await HandleStatusAsync(customerId, deviceIdentifier, payload, ct).ConfigureAwait(false);
                break;
        }
    }

    private async Task HandleTagsAsync(ulong customerId, string deviceIdentifier, string payload, CancellationToken ct)
    {
        MqttTagBatchMessage? batch;
        try
        {
            batch = JsonSerializer.Deserialize<MqttTagBatchMessage>(payload, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid tag batch JSON for reader {Device}.", deviceIdentifier);
            return;
        }

        if (batch?.Events == null || batch.Events.Count == 0)
            return;

        var payloadDevice = (batch.DeviceIdentifier ?? string.Empty).Trim();
        if (payloadDevice.Length > 0 &&
            !string.Equals(payloadDevice, deviceIdentifier, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("deviceIdentifier mismatch topic vs payload for tenant {Customer}.", customerId);
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LaundryMsDbContext>();
        var ingest = scope.ServiceProvider.GetRequiredService<IFixedReaderIngestService>();

        var reader = await db.Readers.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.CustomerId == customerId
                     && r.DeviceIdentifier == deviceIdentifier
                     && r.IsActive,
                ct)
            .ConfigureAwait(false);

        if (reader == null)
        {
            _logger.LogWarning("Unknown reader device {Device} for tenant {Customer}.", deviceIdentifier, customerId);
            return;
        }

        var windowMs = Math.Max(50, _options.IdempotencyWindowMs);

        var byWay = new Dictionary<ulong, List<FixedReaderTagEvent>>();

        foreach (var ev in batch.Events)
        {
            var way = await ResolveReaderWayAsync(db, reader.Id, customerId, ev.Antenna, ct).ConfigureAwait(false);
            if (way == null)
            {
                _logger.LogWarning(
                    "No active scan route for reader {ReaderId} antenna {Antenna}.",
                    reader.Id,
                    ev.Antenna?.ToString() ?? "(none)");
                continue;
            }

            var epc = NormalizeEpc(ev.Epc);
            if (string.IsNullOrEmpty(epc))
                continue;

            var occurredAt = ev.OccurredAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(ev.OccurredAt, DateTimeKind.Utc)
                : ev.OccurredAt.ToUniversalTime();

            var idem = BuildIdempotencyKey(customerId, deviceIdentifier, epc, occurredAt, windowMs);

            if (!byWay.TryGetValue(way.Id, out var list))
            {
                list = [];
                byWay[way.Id] = list;
            }

            list.Add(new FixedReaderTagEvent
            {
                IdempotencyKey = idem,
                RfidTag = epc,
                OccurredAt = occurredAt,
                ConditionAfterEvent = null
            });
        }

        foreach (var kv in byWay)
        {
            var wayEntity = await db.ReaderWays.AsNoTracking()
                .FirstAsync(w => w.Id == kv.Key && w.CustomerId == customerId, ct)
                .ConfigureAwait(false);

            var routeTarget = (wayEntity.TargetProcessStatus ?? string.Empty).Trim().ToLowerInvariant();

            var req = new FixedReaderIngestRequest
            {
                CustomerId = customerId,
                ReaderId = reader.Id,
                ReaderWayId = wayEntity.Id,
                RouteTargetStatus = routeTarget,
                ReaderWayToLocationId = wayEntity.ToLocationId,
                Events = kv.Value,
                IdempotencyWindowMs = windowMs
            };

            var result = await ingest.ProcessBatchAsync(req, ct).ConfigureAwait(false);
            if (!result.Ok)
                _logger.LogWarning("Tag ingest completed with rejections for reader {ReaderId} way {WayId}.", reader.Id, wayEntity.Id);
        }
    }

    private static async Task<ReaderWay?> ResolveReaderWayAsync(
        LaundryMsDbContext db,
        ulong readerId,
        ulong customerId,
        int? antenna,
        CancellationToken ct)
    {
        if (antenna is >= 1 and <= 16)
        {
            var specific = await db.ReaderWays.AsNoTracking()
                .FirstOrDefaultAsync(
                    w => w.ReaderId == readerId
                         && w.CustomerId == customerId
                         && w.IsActive
                         && w.AntennaIndex == antenna.Value,
                    ct)
                .ConfigureAwait(false);
            if (specific != null)
                return specific;
        }

        return await db.ReaderWays.AsNoTracking()
            .FirstOrDefaultAsync(
                w => w.ReaderId == readerId
                     && w.CustomerId == customerId
                     && w.IsActive
                     && w.AntennaIndex == 0,
                ct)
            .ConfigureAwait(false);
    }

    private static string GetPayloadString(MqttApplicationMessage message)
    {
        var segment = message.PayloadSegment;
        if (segment.Count == 0 || segment.Array is null)
            return string.Empty;

        return Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count);
    }

    private static string NormalizeEpc(string? raw)
    {
        var s = (raw ?? string.Empty).Replace(" ", "", StringComparison.Ordinal).Trim();
        return s.Length == 0 ? string.Empty : s.ToUpperInvariant();
    }

    private static string BuildIdempotencyKey(
        ulong customerId,
        string deviceIdentifier,
        string epc,
        DateTime occurredAtUtc,
        int windowMs)
    {
        var bucket = occurredAtUtc.Ticks / (TimeSpan.TicksPerMillisecond * windowMs);
        var raw = $"{customerId}|{deviceIdentifier}|{epc}|{bucket}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash)[..40].ToLowerInvariant();
    }

    private async Task HandleHeartbeatAsync(ulong customerId, string deviceIdentifier, string payload, CancellationToken ct)
    {
        MqttHeartbeatMessage? msg;
        try
        {
            msg = JsonSerializer.Deserialize<MqttHeartbeatMessage>(payload, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid heartbeat JSON.");
            return;
        }

        var sentAt = msg?.SentAt ?? DateTime.UtcNow;
        if (sentAt.Kind == DateTimeKind.Unspecified)
            sentAt = DateTime.SpecifyKind(sentAt, DateTimeKind.Utc);
        else
            sentAt = sentAt.ToUniversalTime();

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LaundryMsDbContext>();
        var registry = scope.ServiceProvider.GetRequiredService<IMqttReaderStateRegistry>();

        var reader = await db.Readers.FirstOrDefaultAsync(
                r => r.CustomerId == customerId && r.DeviceIdentifier == deviceIdentifier && r.IsActive,
                ct)
            .ConfigureAwait(false);

        if (reader == null)
            return;

        reader.LastHeartbeatAt = sentAt;
        reader.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        var minPersist = TimeSpan.FromSeconds(Math.Max(60, _options.HeartbeatDbLogMinSeconds));
        if (registry.ShouldPersistHeartbeat(reader.Id, minPersist))
        {
            db.ReaderEvents.Add(new ReaderEvent
            {
                CustomerId = customerId,
                ReaderId = reader.Id,
                EventType = "mqtt_heartbeat",
                Note = $"fw={msg?.Firmware}; ip={msg?.Ip}; rssiAvg={msg?.RssiAvg}",
                ChangedBy = "mqtt",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    private Task HandleStatusAsync(ulong customerId, string deviceIdentifier, string payload, CancellationToken ct)
    {
        try
        {
            var msg = JsonSerializer.Deserialize<MqttStatusMessage>(payload, JsonOptions);
            var online = string.Equals(msg?.State, "online", StringComparison.OrdinalIgnoreCase);

            using var scope = _scopeFactory.CreateScope();
            var registry = scope.ServiceProvider.GetRequiredService<IMqttReaderStateRegistry>();
            registry.SetConnectionState(customerId, deviceIdentifier, online);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid status JSON.");
        }

        return Task.CompletedTask;
    }
}
