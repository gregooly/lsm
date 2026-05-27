using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using LaundryMS.Web.Data;
using LaundryMS.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Services;

public sealed class LinenEmailReportService : ILinenEmailReportService
{
    private const string RecipientEmailConfigPath = "Reports:RecipientEmail";

    private readonly LaundryMsDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LinenEmailReportService> _logger;

    public LinenEmailReportService(
        LaundryMsDbContext db,
        IConfiguration configuration,
        ILogger<LinenEmailReportService> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public Task TrySendPickupReportAsync(
        ulong tenantCustomerId,
        string pickupLocationDisplayName,
        IReadOnlyDictionary<string, int> itemCountsByType,
        DateTime batchOccurredAtUtc,
        CancellationToken cancellationToken)
        => SendReportAsync(
            tenantCustomerId,
            pickup: true,
            pickupLocationDisplayName,
            itemCountsByType,
            batchOccurredAtUtc,
            cancellationToken);

    public Task TrySendArrivalReportAsync(
        ulong tenantCustomerId,
        IReadOnlyDictionary<string, int> itemCountsByType,
        DateTime batchOccurredAtUtc,
        CancellationToken cancellationToken)
        => SendReportAsync(
            tenantCustomerId,
            pickup: false,
            pickupLocation: null,
            itemCountsByType,
            batchOccurredAtUtc,
            cancellationToken);

    private async Task SendReportAsync(
        ulong tenantCustomerId,
        bool pickup,
        string? pickupLocation,
        IReadOnlyDictionary<string, int> itemCountsByType,
        DateTime batchOccurredAtUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            if (itemCountsByType.Count == 0)
                return;

            var settings = await _db.SystemSettings.AsNoTracking()
                .Where(x => x.CustomerId == tenantCustomerId)
                .ToDictionaryAsync(x => x.SettingKey, x => x.SettingValue, cancellationToken);

            bool Bool(string key) => bool.TryParse(settings.GetValueOrDefault(key), out var b) && b;

            if (!Bool(SettingsKeys.EmailReportsEnabled))
                return;

            if (pickup && !Bool(SettingsKeys.SendPickupReport))
                return;

            if (!pickup && !Bool(SettingsKeys.SendArrivalReport))
                return;

            var host = (settings.GetValueOrDefault(SettingsKeys.SmtpHost) ?? string.Empty).Trim();
            if (!int.TryParse(settings.GetValueOrDefault(SettingsKeys.SmtpPort), out var port) || port <= 0)
                return;

            var username = (settings.GetValueOrDefault(SettingsKeys.SmtpUsername) ?? string.Empty).Trim();
            var password = settings.GetValueOrDefault(SettingsKeys.SmtpPassword) ?? string.Empty;
            var from = (settings.GetValueOrDefault(SettingsKeys.DefaultFromAddress) ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
                return;

            var to = await ResolveRecipientEmailAsync(tenantCustomerId, cancellationToken);
            if (string.IsNullOrWhiteSpace(to))
            {
                _logger.LogWarning("Email report skipped: no recipient for tenant {TenantId}.", tenantCustomerId);
                return;
            }

            var timeDisplay = FormatLocalTimestamp(batchOccurredAtUtc);
            var bulletedItems = BuildItemBullets(itemCountsByType);

            string subject;
            string body;
            if (pickup)
            {
                subject = "Laundry Pickup Confirmation";
                var loc = string.IsNullOrWhiteSpace(pickupLocation) ? "Not specified" : pickupLocation.Trim();
                body = $"""
                    Dear Customer,

                    Your laundry items have been successfully collected by the driver.

                    Pickup Location: {loc}

                    Collected Items:
                    {bulletedItems}

                    Pickup Time: {timeDisplay}

                    Your items are now in transit to the laundry facility.

                    Thank you.
                    """;
            }
            else
            {
                subject = "Laundry Arrival Confirmation";
                body = $"""
                    Dear Customer,

                    Your laundry items have arrived at the laundry facility and were successfully verified by the RFID gate system.

                    Verified Items:
                    {bulletedItems}

                    Arrival Time: {timeDisplay}

                    The cleaning process will now begin.

                    Thank you.
                    """;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(from),
                Subject = subject,
                Body = body.Trim().ReplaceLineEndings("\r\n"),
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8,
                IsBodyHtml = false
            };
            message.To.Add(to.Trim());

            using var smtp = new SmtpClient(host, port)
            {
                EnableSsl = port is 465 or 587,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 15000
            };

            if (!string.IsNullOrEmpty(username))
                smtp.Credentials = new NetworkCredential(username, password);

            await smtp.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(
                "Sent {ReportKind} email to tenant {TenantId}.",
                pickup ? "pickup" : "arrival",
                tenantCustomerId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email report failed for tenant {TenantId}; ingest left unaffected.", tenantCustomerId);
        }
    }

    private async Task<string?> ResolveRecipientEmailAsync(ulong tenantCustomerId, CancellationToken cancellationToken)
    {
        var overrideEmail = (_configuration[RecipientEmailConfigPath] ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(overrideEmail))
            return overrideEmail;

        var byRowId = await _db.Customers.AsNoTracking()
            .Where(c => c.Id == tenantCustomerId)
            .Select(c => c.PrimaryEmail)
            .FirstOrDefaultAsync(cancellationToken);

        var trimmed = (byRowId ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
            return trimmed;

        var byExternalId = await _db.Customers.AsNoTracking()
            .Where(c => c.CustomerId == tenantCustomerId)
            .Select(c => c.PrimaryEmail)
            .FirstOrDefaultAsync(cancellationToken);

        trimmed = (byExternalId ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string FormatLocalTimestamp(DateTime utc)
    {
        var utcStamp = utc.Kind == DateTimeKind.Utc
            ? utc
            : DateTime.SpecifyKind(utc.ToUniversalTime(), DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utcStamp, TimeZoneInfo.Local);
        return local.ToString("yyyy-MM-dd hh:mm tt", CultureInfo.InvariantCulture);
    }

    private static string BuildItemBullets(IReadOnlyDictionary<string, int> itemCountsByType)
    {
        var sb = new StringBuilder();
        foreach (var pair in itemCountsByType.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            var label = string.IsNullOrWhiteSpace(pair.Key) ? "item" : pair.Key.Trim();
            sb.Append("- ").Append(pair.Value).Append(' ').Append(label).AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}
