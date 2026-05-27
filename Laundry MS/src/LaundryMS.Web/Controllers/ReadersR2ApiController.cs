using LaundryMS.Web.Auth;
using LaundryMS.Web.Data;
using LaundryMS.Web.Models.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Controllers;

/// <summary>Mobile R2 reader connection reporting.</summary>
[ApiController]
[Route("api/readers/r2")]
[Authorize(Roles = "DRIVER")]
public class ReadersR2ApiController : ControllerBase
{
    private readonly LaundryMsDbContext _db;

    public ReadersR2ApiController(LaundryMsDbContext db)
    {
        _db = db;
    }

    /// <summary>Called when Bluetooth connection to the R2 succeeds.</summary>
    [HttpPost("connection-status")]
    public async Task<IActionResult> ConnectionStatus([FromBody] R2ConnectionRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetDriverCustomerAndId(out var tenantCustomerId, out var tokenDriverId))
            return Unauthorized(new { success = false, message = "Invalid driver token." });

        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid request body." });

        var handheld = (request.HandheldId ?? string.Empty).Trim();
        if (handheld.Length == 0)
            return BadRequest(new { success = false, message = "handheldId is required." });

        if (request.CustomerId != tenantCustomerId || request.DriverId != tokenDriverId)
            return BadRequest(new { success = false, message = "Driver or customer does not match token." });
    
        var driver = await _db.Drivers.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == request.DriverId && x.CustomerId == tenantCustomerId && x.IsActive,
                cancellationToken);

        if (driver == null)
            return BadRequest(new { success = false, message = "Driver not found." });

        var reader = await _db.Readers
            .FirstOrDefaultAsync(
                x => x.CustomerId == tenantCustomerId
                     && x.DeviceIdentifier == handheld
                     && x.IsActive,
                cancellationToken);

        if (reader == null)
        {
            return NotFound(new
            {
                success = false,
                message = "No reader registered with this device identifier. Add a reader in Readers with Device identifier matching the R2 handheld id."
            });
        }

        var connectedAt = request.ConnectedAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(request.ConnectedAt, DateTimeKind.Utc)
            : request.ConnectedAt.ToUniversalTime();

        reader.LastHeartbeatAt = connectedAt;
        reader.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            readerId = reader.Id
        });
    }
}
