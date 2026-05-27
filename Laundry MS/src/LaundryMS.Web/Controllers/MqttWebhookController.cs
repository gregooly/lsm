using System.Text.Json;
using LaundryMS.Web.Data;
using LaundryMS.Web.Options;
using LaundryMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LaundryMS.Web.Controllers;

/// <summary>EMQX HTTP authentication / authorization (Strategy B). Protected by shared webhook secret header.</summary>
[ApiController]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/mqtt")]
public sealed class MqttWebhookController : ControllerBase
{
    public const string WebhookSecretHeaderName = "X-LaundryMS-Mqtt-Webhook-Secret";

    private readonly LaundryMsDbContext _db;
    private readonly MqttOptions _mqtt;

    public MqttWebhookController(LaundryMsDbContext db, IOptions<MqttOptions> mqtt)
    {
        _db = db;
        _mqtt = mqtt.Value;
    }

    /// <summary>EMQX HTTP authentication plugin POST.</summary>
    [HttpPost("auth")]
    public async Task<IActionResult> Authenticate(CancellationToken cancellationToken)
    {
        if (!ValidateSecret())
            return Unauthorized();

        JsonDocument doc;
        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
        }
        catch (JsonException)
        {
            return Ok(new { result = "deny" });
        }

        var root = doc.RootElement;
        var username = GetStringProp(root, "username", "user_name", "userName");
        var password = GetStringProp(root, "password");
        var clientId = GetStringProp(root, "clientid", "client_id", "clientId");

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return Ok(new { result = "deny" });

        var readerRow = await _db.Readers.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.MqttUsername == username && x.IsActive && x.CustomerId != null,
                cancellationToken)
            .ConfigureAwait(false);

        if (readerRow == null || string.IsNullOrWhiteSpace(readerRow.MqttPasswordHash))
            return Ok(new { result = "deny" });

        if (!BCrypt.Net.BCrypt.Verify(password, readerRow.MqttPasswordHash))
            return Ok(new { result = "deny" });

        if (!string.IsNullOrWhiteSpace(clientId)
            && !string.Equals(clientId.Trim(), readerRow.DeviceIdentifier.Trim(), StringComparison.Ordinal))
            return Ok(new { result = "deny" });

        return Ok(new { result = "allow", is_superuser = false });
    }

    /// <summary>EMQX HTTP authorization (ACL) plugin POST.</summary>
    [HttpPost("acl")]
    public async Task<IActionResult> Authorize(CancellationToken cancellationToken)
    {
        if (!ValidateSecret())
            return Unauthorized();

        JsonDocument doc;
        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
        }
        catch (JsonException)
        {
            return Ok(new { result = "deny" });
        }

        var root = doc.RootElement;
        var username = GetStringProp(root, "username", "user_name", "userName");
        var topic = GetStringProp(root, "topic");
        var action = GetStringProp(root, "action");
        var clientId = GetStringProp(root, "clientid", "client_id", "clientId");

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(topic))
            return Ok(new { result = "deny" });

        var prefix = _mqtt.TopicPrefix.Trim().Trim('/');
        var subUser = (_mqtt.SubscriberUsername ?? string.Empty).Trim();
        if (subUser.Length > 0 && string.Equals(username, subUser, StringComparison.Ordinal))
        {
            var pfx = prefix + "/";
            if (topic.StartsWith(pfx, StringComparison.Ordinal))
                return Ok(new { result = "allow" });
        }

        var readerRow = await _db.Readers.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.MqttUsername == username && x.IsActive && x.CustomerId != null,
                cancellationToken)
            .ConfigureAwait(false);

        if (readerRow == null)
            return Ok(new { result = "deny" });

        if (!string.IsNullOrWhiteSpace(clientId)
            && !string.Equals(clientId.Trim(), readerRow.DeviceIdentifier.Trim(), StringComparison.Ordinal))
            return Ok(new { result = "deny" });

        if (!MqttTopicPaths.TryParseReaderTopic(prefix, topic, out var customerId, out var deviceId, out var suffix))
            return Ok(new { result = "deny" });

        if (readerRow.CustomerId != customerId
            || !string.Equals(deviceId, readerRow.DeviceIdentifier, StringComparison.Ordinal))
            return Ok(new { result = "deny" });

        var act = (action ?? string.Empty).Trim().ToLowerInvariant();
        var suf = suffix.Trim().ToLowerInvariant();

        var isPublish = act is "publish" or "pub";
        var isSubscribe = act is "subscribe" or "sub";

        if (isPublish && suf is "tags" or "heartbeat" or "status")
            return Ok(new { result = "allow" });

        if (isSubscribe && suf is "cmd" or "config")
            return Ok(new { result = "allow" });

        return Ok(new { result = "deny" });
    }

    private bool ValidateSecret()
    {
        var expected = (_mqtt.WebhookSharedSecret ?? string.Empty).Trim();
        if (expected.Length == 0)
            return false;

        if (!Request.Headers.TryGetValue(WebhookSecretHeaderName, out var sent))
            return false;

        return string.Equals(sent.ToString().Trim(), expected, StringComparison.Ordinal);
    }

    private static string? GetStringProp(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var el))
                continue;

            return el.ValueKind switch
            {
                JsonValueKind.String => el.GetString(),
                JsonValueKind.Number => el.GetRawText(),
                _ => null
            };
        }

        return null;
    }
}
