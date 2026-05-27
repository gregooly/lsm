using LaundryMS.Web.Auth;
using LaundryMS.Web.Data;
using LaundryMS.Web.Models.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Controllers;

/// <summary>Table app Bluetooth reader verification.</summary>
[ApiController]
[Route("api/readers/table")]
[Authorize(Roles = AuthConstants.TableRole)]
public class ReadersTableApiController : ControllerBase
{
    private readonly LaundryMsDbContext _db;

    public ReadersTableApiController(LaundryMsDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Called when Bluetooth connects to the Chainway reader.
    /// Body: handheldId, customerId, connectedAt only. Returns readerId for the app to store.
    /// </summary>
    [HttpPost("connection-status")]
    public async Task<IActionResult> ConnectionStatus([FromBody] TableConnectionRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetTableCustomerAndDriverId(out var tenantCustomerId, out _))
            return Unauthorized(new { success = false, message = "Invalid table token." });

        var handheld = (request.HandheldId ?? request.DeviceId ?? string.Empty).Trim();
        if (handheld.Length == 0)
            return BadRequest(new { success = false, message = "handheldId is required." });

        if (request.CustomerId == 0 || request.CustomerId != tenantCustomerId)
            return BadRequest(new { success = false, message = "Customer does not match token." });

        var reader = await _db.Readers
            .FirstOrDefaultAsync(
                x => x.CustomerId == tenantCustomerId
                     && x.DeviceIdentifier.Trim() == handheld
                     && x.IsActive,
                cancellationToken)
            .ConfigureAwait(false);

        if (reader == null)
        {
            return NotFound(new
            {
                success = false,
                message = "No reader registered with this device identifier. Add a reader in Readers with Device identifier matching the Chainway handheld id."
            });
        }

        var connectedAt = request.ConnectedAt == default
            ? DateTime.UtcNow
            : request.ConnectedAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.ConnectedAt, DateTimeKind.Utc)
                : request.ConnectedAt.ToUniversalTime();

        reader.LastHeartbeatAt = connectedAt;
        reader.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Ok(new
        {
            success = true,
            readerId = reader.Id
        });
    }
}
