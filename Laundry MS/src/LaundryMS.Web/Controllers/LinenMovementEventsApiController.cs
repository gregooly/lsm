using LaundryMS.Web.Auth;
using LaundryMS.Web.Models.Api;
using LaundryMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMS.Web.Controllers;

/// <summary>Ingest RFID scan batches from the driver R2 app.</summary>
[ApiController]
[Route("api/linen-movement-events")]
[Authorize(Roles = "DRIVER")]
public class LinenMovementEventsApiController : ControllerBase
{
    private readonly DriverLinenIngestService _ingest;

    public LinenMovementEventsApiController(DriverLinenIngestService ingest)
    {
        _ingest = ingest;
    }

    [HttpPost]
    public Task<IActionResult> PostBatch([FromBody] LinenMovementBatchRequest request, CancellationToken cancellationToken)
        => ProcessInternal(request, cancellationToken);

    /// <summary>Same processing as POST / ; used for “sync all” from the offline queue.</summary>
    [HttpPost("sync")]
    public Task<IActionResult> SyncBatch([FromBody] LinenMovementBatchRequest request, CancellationToken cancellationToken)
        => ProcessInternal(request, cancellationToken);

    private async Task<IActionResult> ProcessInternal(LinenMovementBatchRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetDriverCustomerAndId(out var tenantCustomerId, out var driverId))
            return Unauthorized(new { success = false, message = "Invalid driver token." });

        if (request.Events == null || request.Events.Count == 0)
            return BadRequest(new { success = false, message = "events[] is required." });

        var result = await _ingest.ProcessBatchAsync(request, tenantCustomerId, driverId, cancellationToken);

        if (!result.Ok && result.Results.Count == 0)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new
        {
            success = result.Ok,
            message = result.Message,
            results = result.Results
        });
    }
}
