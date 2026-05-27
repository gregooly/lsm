using LaundryMS.Web.Auth;
using LaundryMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMS.Web.Controllers;

[ApiController]
[Route("api/table")]
[Authorize(Roles = AuthConstants.TableRole)]
public class TableApiController : ControllerBase
{
    private readonly ITableBootstrapService _bootstrapService;

    public TableApiController(ITableBootstrapService bootstrapService)
    {
        _bootstrapService = bootstrapService;
    }

    /// <summary>Load scan routes for the Bluetooth-linked reader (pass readerId from connection-status).</summary>
    [HttpGet("bootstrap")]
    public async Task<IActionResult> Bootstrap([FromQuery] ulong readerId, CancellationToken cancellationToken)
    {
        if (!User.TryGetTableCustomerAndDriverId(out var customerId, out _))
            return Unauthorized(new { success = false, message = "Invalid table token." });

        if (readerId == 0)
            return BadRequest(new { success = false, message = "readerId query parameter is required." });

        var data = await _bootstrapService.GetAsync(customerId, readerId, cancellationToken).ConfigureAwait(false);
        if (data == null)
            return NotFound(new { success = false, message = "Reader not found or inactive." });

        return Ok(new
        {
            success = true,
            reader = data.Reader,
            readerWays = data.ReaderWays,
            locations = data.Locations,
            syncPolicy = data.SyncPolicy
        });
    }
}
