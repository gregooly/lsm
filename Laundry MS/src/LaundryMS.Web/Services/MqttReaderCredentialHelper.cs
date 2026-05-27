using LaundryMS.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Services;

public static class MqttReaderCredentialHelper
{
    private static readonly char[] PasswordAlphabet =
        "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789".ToCharArray();

    public static string BuildDefaultMqttUsername(ulong customerId, string deviceIdentifier)
    {
        var slug = new string((deviceIdentifier ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        if (slug.Length > 40)
            slug = slug[..40];
        if (slug.Length == 0)
            slug = "reader";

        var u = $"r-{customerId}-{slug}";
        return u.Length <= 64 ? u : u[..64];
    }

    public static string GeneratePlainPassword(int length = 24)
        => new string(Random.Shared.GetItems(PasswordAlphabet, length));

    /// <summary>Ensure unique <paramref name="baseUsername"/> against DB (excluding <paramref name="readerId"/>).</summary>
    public static async Task<string> EnsureUniqueMqttUsernameAsync(
        LaundryMsDbContext db,
        string baseUsername,
        ulong readerId,
        CancellationToken cancellationToken)
    {
        var candidate = baseUsername;
        for (var i = 0; i < 50; i++)
        {
            var taken = await db.Readers.AsNoTracking()
                .AnyAsync(x => x.MqttUsername == candidate && x.Id != readerId, cancellationToken)
                .ConfigureAwait(false);
            if (!taken)
                return candidate;

            var suffix = Random.Shared.Next(0, 0x10000).ToString("x4");
            var combined = $"{baseUsername}-{suffix}";
            candidate = combined.Length <= 64 ? combined : combined[..64];
        }

        throw new InvalidOperationException("Could not allocate a unique MQTT username.");
    }
}
