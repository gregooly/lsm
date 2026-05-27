using LaundryMS.Web.Auth;
using LaundryMS.Web.Models.Api;
using LaundryMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMS.Web.Controllers;

/// <summary>Ingest RFID scan batches from the table reader app (good / damaged / lost).</summary>
[ApiController]
[Route("api/table/linen-movement-events")]
[Authorize(Roles = AuthConstants.TableRole)]
public class TableLinenMovementEventsApiController : ControllerBase
{
    private readonly TableLinenIngestService _ingest;

    public TableLinenMovementEventsApiController(TableLinenIngestService ingest)
    {
        _ingest = ingest;
    }

    [HttpPost]
    public Task<IActionResult> PostBatch([FromBody] TableLinenMovementBatchRequest request, CancellationToken cancellationToken)
        => ProcessInternal(request, cancellationToken);

    [HttpPost("sync")]
    public Task<IActionResult> SyncBatch([FromBody] TableLinenMovementBatchRequest request, CancellationToken cancellationToken)
        => ProcessInternal(request, cancellationToken);

    private async Task<IActionResult> ProcessInternal(TableLinenMovementBatchRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetTableCustomerAndDriverId(out var tenantCustomerId, out var driverId))
            return Unauthorized(new { success = false, message = "Invalid table token." });

        if (request.Events == null || request.Events.Count == 0)
            return BadRequest(new { success = false, message = "events[] is required." });

        var result = await _ingest.ProcessBatchAsync(request, tenantCustomerId, driverId, cancellationToken)
            .ConfigureAwait(false);

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
