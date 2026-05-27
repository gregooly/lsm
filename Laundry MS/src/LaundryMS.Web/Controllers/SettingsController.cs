using LaundryMS.Web.Data;
using LaundryMS.Web.Models.Entities;
using LaundryMS.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

namespace LaundryMS.Web.Controllers;

public class SettingsController : TenantScopedController
{
    private readonly LaundryMsDbContext _dbContext;

    public SettingsController(LaundryMsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var map = await _dbContext.SystemSettings.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .ToDictionaryAsync(x => x.SettingKey, x => x, cancellationToken);

        string? Get(string key) => map.TryGetValue(key, out var s) ? s.SettingValue : null;
        bool GetBool(string key) => bool.TryParse(Get(key), out var b) && b;
        int? GetInt(string key) => int.TryParse(Get(key), out var i) ? i : null;

        var vm = new SettingsIndexViewModel
        {
            DeviceIngestEnabled = GetBool(SettingsKeys.DeviceIngestEnabled),
            DeviceIngestApiKey = null,
            DeviceIngestApiKeyConfigured = !string.IsNullOrWhiteSpace(Get(SettingsKeys.DeviceIngestApiKey)),
            WebhookUrl = Get(SettingsKeys.WebhookUrl),

            EmailReportsEnabled = GetBool(SettingsKeys.EmailReportsEnabled),
            SmtpHost = Get(SettingsKeys.SmtpHost),
            SmtpPort = GetInt(SettingsKeys.SmtpPort),
            SmtpUsername = Get(SettingsKeys.SmtpUsername),
            SmtpPassword = null,
            SmtpPasswordConfigured = !string.IsNullOrWhiteSpace(Get(SettingsKeys.SmtpPassword)),
            DefaultFromAddress = Get(SettingsKeys.DefaultFromAddress),

            SendPickupReport = GetBool(SettingsKeys.SendPickupReport),
            SendArrivalReport = GetBool(SettingsKeys.SendArrivalReport),
            SendDispatchReport = GetBool(SettingsKeys.SendDispatchReport),

            WorkflowClearTemporaryEmployeeOnStatus = Get(SettingsKeys.WorkflowClearTemporaryEmployeeOnStatus)
                ?? "cleaned,ready_for_dispatch,at_customer",
            LastUpdatedAtUtc = map.Values
                .OrderByDescending(x => x.UpdatedAt)
                .Select(x => (DateTime?)x.UpdatedAt)
                .FirstOrDefault()
        };

        ViewData["Saved"] = TempData["Saved"];
        ViewData["SettingsError"] = TempData["SettingsError"];
        ViewData["TestResult"] = TempData["TestResult"];
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SettingsIndexViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        TryValidateModel(model);
        var existing = await _dbContext.SystemSettings
            .Where(x => x.CustomerId == customerId)
            .ToDictionaryAsync(x => x.SettingKey, x => x, cancellationToken);

        ApplyBusinessValidation(model, existing);
        if (!ModelState.IsValid)
        {
            HydrateConfiguredFlags(model, existing);
            return View("Index", model);
        }

        void Upsert(string key, string value, bool isSecret = false)
        {
            if (existing.TryGetValue(key, out var row))
            {
                row.SettingValue = value;
                row.IsSecret = isSecret;
                row.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _dbContext.SystemSettings.Add(new SystemSetting
                {
                    CustomerId = customerId,
                    SettingKey = key,
                    SettingValue = value,
                    IsSecret = isSecret,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        Upsert(SettingsKeys.DeviceIngestEnabled, model.DeviceIngestEnabled.ToString());
        var incomingApiKey = model.DeviceIngestApiKey?.Trim();
        if (!string.IsNullOrWhiteSpace(incomingApiKey))
        {
            Upsert(SettingsKeys.DeviceIngestApiKey, incomingApiKey, isSecret: true);
        }
        else if (!existing.ContainsKey(SettingsKeys.DeviceIngestApiKey))
        {
            Upsert(SettingsKeys.DeviceIngestApiKey, "", isSecret: true);
        }
        Upsert(SettingsKeys.WebhookUrl, model.WebhookUrl?.Trim() ?? "");

        Upsert(SettingsKeys.EmailReportsEnabled, model.EmailReportsEnabled.ToString());
        Upsert(SettingsKeys.SmtpHost, model.SmtpHost?.Trim() ?? "");
        Upsert(SettingsKeys.SmtpPort, (model.SmtpPort ?? 0).ToString());
        Upsert(SettingsKeys.SmtpUsername, model.SmtpUsername?.Trim() ?? "");
        var incomingSmtpPassword = model.SmtpPassword?.Trim();
        if (!string.IsNullOrWhiteSpace(incomingSmtpPassword))
        {
            Upsert(SettingsKeys.SmtpPassword, incomingSmtpPassword, isSecret: true);
        }
        else if (!existing.ContainsKey(SettingsKeys.SmtpPassword))
        {
            Upsert(SettingsKeys.SmtpPassword, "", isSecret: true);
        }
        Upsert(SettingsKeys.DefaultFromAddress, model.DefaultFromAddress?.Trim() ?? "");

        Upsert(SettingsKeys.SendPickupReport, model.SendPickupReport.ToString());
        Upsert(SettingsKeys.SendArrivalReport, model.SendArrivalReport.ToString());
        Upsert(SettingsKeys.SendDispatchReport, model.SendDispatchReport.ToString());

        Upsert(SettingsKeys.WorkflowClearTemporaryEmployeeOnStatus, model.WorkflowClearTemporaryEmployeeOnStatus?.Trim() ?? "");

        await _dbContext.SaveChangesAsync(cancellationToken);

        TempData["Saved"] = "Settings saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestSmtp(SettingsIndexViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var existing = await _dbContext.SystemSettings.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .ToDictionaryAsync(x => x.SettingKey, x => x, cancellationToken);
        ApplyBusinessValidation(model, existing, validateDeviceIngest: false);
        if (!ModelState.IsValid)
        {
            HydrateConfiguredFlags(model, existing);
            TempData["SettingsError"] = "SMTP test was not run due to validation errors.";
            return View("Index", model);
        }

        var smtpPassword = !string.IsNullOrWhiteSpace(model.SmtpPassword)
            ? model.SmtpPassword.Trim()
            : existing.TryGetValue(SettingsKeys.SmtpPassword, out var secret) ? secret.SettingValue : "";

        try
        {
            using var smtp = new SmtpClient(model.SmtpHost!.Trim(), model.SmtpPort!.Value)
            {
                EnableSsl = model.SmtpPort.Value == 465 || model.SmtpPort.Value == 587,
                Credentials = new NetworkCredential(model.SmtpUsername!.Trim(), smtpPassword),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 7000
            };

            await smtp.SendMailAsync(
                from: model.DefaultFromAddress!.Trim(),
                recipients: model.DefaultFromAddress.Trim(),
                subject: "LaundryMS settings test",
                body: "SMTP settings test successful.",
                cancellationToken);

            TempData["TestResult"] = "SMTP test succeeded. A test message was sent to the default from-address.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            HydrateConfiguredFlags(model, existing);
            TempData["SettingsError"] = $"SMTP test failed: {ex.Message}";
            return View("Index", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestWebhook(SettingsIndexViewModel model, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var existing = await _dbContext.SystemSettings.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .ToDictionaryAsync(x => x.SettingKey, x => x, cancellationToken);

        if (string.IsNullOrWhiteSpace(model.WebhookUrl))
        {
            ModelState.AddModelError(nameof(model.WebhookUrl), "Webhook URL is required to run webhook test.");
            HydrateConfiguredFlags(model, existing);
            return View("Index", model);
        }

        if (!Uri.TryCreate(model.WebhookUrl.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            ModelState.AddModelError(nameof(model.WebhookUrl), "Webhook URL must be a valid HTTP/HTTPS URL.");
            HydrateConfiguredFlags(model, existing);
            return View("Index", model);
        }

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(7) };
            using var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(
                    "{\"event\":\"settings_webhook_test\",\"source\":\"LaundryMS\",\"sentAtUtc\":\"" + DateTime.UtcNow.ToString("o") + "\"}",
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
            var response = await http.SendAsync(request, cancellationToken);
            TempData["TestResult"] = response.IsSuccessStatusCode
                ? $"Webhook test succeeded ({(int)response.StatusCode})."
                : $"Webhook test reached endpoint but returned {(int)response.StatusCode}.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            HydrateConfiguredFlags(model, existing);
            TempData["SettingsError"] = $"Webhook test failed: {ex.Message}";
            return View("Index", model);
        }
    }

    private void ApplyBusinessValidation(
        SettingsIndexViewModel model,
        IReadOnlyDictionary<string, SystemSetting> existing,
        bool validateDeviceIngest = true)
    {
        if (validateDeviceIngest && model.DeviceIngestEnabled)
        {
            var hasIncoming = !string.IsNullOrWhiteSpace(model.DeviceIngestApiKey);
            var hasStored = existing.TryGetValue(SettingsKeys.DeviceIngestApiKey, out var apiKeyRow)
                && !string.IsNullOrWhiteSpace(apiKeyRow.SettingValue);
            if (!hasIncoming && !hasStored)
            {
                ModelState.AddModelError(nameof(model.DeviceIngestApiKey), "API key is required when device ingest is enabled.");
            }
        }

        if (!string.IsNullOrWhiteSpace(model.WebhookUrl))
        {
            var ok = Uri.TryCreate(model.WebhookUrl.Trim(), UriKind.Absolute, out var webhookUri)
                     && (webhookUri.Scheme == Uri.UriSchemeHttp || webhookUri.Scheme == Uri.UriSchemeHttps);
            if (!ok)
            {
                ModelState.AddModelError(nameof(model.WebhookUrl), "Webhook URL must be a valid HTTP/HTTPS URL.");
            }
        }

        if (model.EmailReportsEnabled)
        {
            if (string.IsNullOrWhiteSpace(model.SmtpHost))
                ModelState.AddModelError(nameof(model.SmtpHost), "SMTP host is required when email reports are enabled.");
            if (!model.SmtpPort.HasValue)
                ModelState.AddModelError(nameof(model.SmtpPort), "SMTP port is required when email reports are enabled.");
            if (string.IsNullOrWhiteSpace(model.SmtpUsername))
                ModelState.AddModelError(nameof(model.SmtpUsername), "SMTP username is required when email reports are enabled.");
            if (string.IsNullOrWhiteSpace(model.DefaultFromAddress))
                ModelState.AddModelError(nameof(model.DefaultFromAddress), "Default from-address is required when email reports are enabled.");

            var hasIncomingPassword = !string.IsNullOrWhiteSpace(model.SmtpPassword);
            var hasStoredPassword = existing.TryGetValue(SettingsKeys.SmtpPassword, out var smtpPasswordRow)
                && !string.IsNullOrWhiteSpace(smtpPasswordRow.SettingValue);
            if (!hasIncomingPassword && !hasStoredPassword)
            {
                ModelState.AddModelError(nameof(model.SmtpPassword), "SMTP password is required when email reports are enabled.");
            }
        }
    }

    private static void HydrateConfiguredFlags(SettingsIndexViewModel model, IReadOnlyDictionary<string, SystemSetting> existing)
    {
        model.DeviceIngestApiKey = null;
        model.SmtpPassword = null;
        model.DeviceIngestApiKeyConfigured = existing.TryGetValue(SettingsKeys.DeviceIngestApiKey, out var apiKeyRow)
            && !string.IsNullOrWhiteSpace(apiKeyRow.SettingValue);
        model.SmtpPasswordConfigured = existing.TryGetValue(SettingsKeys.SmtpPassword, out var smtpRow)
            && !string.IsNullOrWhiteSpace(smtpRow.SettingValue);
        model.LastUpdatedAtUtc = existing.Values
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => (DateTime?)x.UpdatedAt)
            .FirstOrDefault();
    }
}
