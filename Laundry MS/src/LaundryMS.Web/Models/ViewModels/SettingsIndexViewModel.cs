using System.ComponentModel.DataAnnotations;

namespace LaundryMS.Web.Models.ViewModels;

public class SettingsIndexViewModel
{
    // Integrations
    public bool DeviceIngestEnabled { get; set; }

    [StringLength(200)]
    public string? DeviceIngestApiKey { get; set; }

    [StringLength(500)]
    public string? WebhookUrl { get; set; }

    // Email reports
    public bool EmailReportsEnabled { get; set; }

    [StringLength(150)]
    public string? SmtpHost { get; set; }

    [Range(1, 65535)]
    public int? SmtpPort { get; set; }

    [StringLength(150)]
    public string? SmtpUsername { get; set; }

    [StringLength(200)]
    public string? SmtpPassword { get; set; }

    [StringLength(150)]
    [EmailAddress]
    public string? DefaultFromAddress { get; set; }

    public bool SendPickupReport { get; set; }
    public bool SendArrivalReport { get; set; }
    public bool SendDispatchReport { get; set; }

    /// <summary>Comma-separated process status values; when temporary-assigned linen reaches one of these, employee assignee is cleared.</summary>
    [StringLength(500)]
    public string? WorkflowClearTemporaryEmployeeOnStatus { get; set; }

    public bool DeviceIngestApiKeyConfigured { get; set; }
    public bool SmtpPasswordConfigured { get; set; }
    public DateTime? LastUpdatedAtUtc { get; set; }
}

public static class SettingsKeys
{
    public const string DeviceIngestEnabled = "integrations.device_ingest.enabled";
    public const string DeviceIngestApiKey = "integrations.device_ingest.api_key";
    public const string WebhookUrl = "integrations.webhook.url";

    public const string EmailReportsEnabled = "reports.email.enabled";
    public const string SmtpHost = "reports.email.smtp.host";
    public const string SmtpPort = "reports.email.smtp.port";
    public const string SmtpUsername = "reports.email.smtp.username";
    public const string SmtpPassword = "reports.email.smtp.password";
    public const string DefaultFromAddress = "reports.email.from";

    public const string SendPickupReport = "reports.email.pickup.enabled";
    public const string SendArrivalReport = "reports.email.arrival.enabled";
    public const string SendDispatchReport = "reports.email.dispatch.enabled";

    public const string WorkflowClearTemporaryEmployeeOnStatus = "workflow.temporary.clear_employee_on_process_status";
}

