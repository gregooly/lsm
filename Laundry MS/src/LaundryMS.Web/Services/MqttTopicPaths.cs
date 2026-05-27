namespace LaundryMS.Web.Services;

public static class MqttTopicPaths
{
    public static bool TryParseReaderTopic(string topicPrefix, string topic, out ulong customerId, out string deviceIdentifier, out string suffix)
    {
        customerId = 0;
        deviceIdentifier = string.Empty;
        suffix = string.Empty;

        var prefix = topicPrefix.Trim().Trim('/');
        var parts = topic.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 5)
            return false;

        if (!string.Equals(parts[0], prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!ulong.TryParse(parts[1], out customerId))
            return false;

        if (!string.Equals(parts[2], "readers", StringComparison.OrdinalIgnoreCase))
            return false;

        deviceIdentifier = parts[3];
        suffix = parts[^1];
        return !string.IsNullOrEmpty(deviceIdentifier);
    }

    public static string ExpectedPublishSuffixTags => "tags";

    public static string ExpectedPublishSuffixHeartbeat => "heartbeat";

    public static string ExpectedPublishSuffixStatus => "status";

    public static string ExpectedSubscribeSuffixCmd => "cmd";

    public static string ExpectedSubscribeSuffixConfig => "config";
}
