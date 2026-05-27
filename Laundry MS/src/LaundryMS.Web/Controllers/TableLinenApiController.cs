using LaundryMS.Web.Auth;
using LaundryMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMS.Web.Controllers;

[ApiController]
[Route("api/table/linen")]
[Authorize(Roles = AuthConstants.TableRole)]
public class TableLinenApiController : ControllerBase
{
    private readonly TableLinenIngestService _linen;

    public TableLinenApiController(TableLinenIngestService linen)
    {
        _linen = linen;
    }

    [HttpGet("by-tag/{rfidTag}")]
    public async Task<IActionResult> GetByTag(
        string rfidTag,
        [FromQuery] ulong readerId,
        [FromQuery] ulong? readerWayId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetTableCustomerAndDriverId(out var customerId, out _))
            return Unauthorized(new { success = false, message = "Invalid table token." });

        if (readerId == 0)
            return BadRequest(new { success = false, message = "readerId query parameter is required." });

        var decoded = Uri.UnescapeDataString(rfidTag ?? string.Empty);
        var result = await _linen.LookupByTagAsync(customerId, readerId, decoded, readerWayId, cancellationToken)
            .ConfigureAwait(false);

        return Ok(new
        {
            success = true,
            found = result.Found,
            item = result.Item,
            warnings = result.Warnings
        });
    }
}
