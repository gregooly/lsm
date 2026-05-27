using LaundryMS.Web.Auth;
using LaundryMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMS.Web.Controllers;

[ApiController]
[Route("api/driver")]
[Authorize(Roles = "DRIVER")]
public class DriverApiController : ControllerBase
{
    private readonly IDriverBootstrapService _bootstrapService;

    public DriverApiController(IDriverBootstrapService bootstrapService)
    {
        _bootstrapService = bootstrapService;
    }

    /// <summary>Reload tasks, locations, and scan routes after login or on demand.</summary>
    [HttpGet("bootstrap")]
    public async Task<IActionResult> Bootstrap(CancellationToken cancellationToken)
    {
        if (!User.TryGetDriverCustomerAndId(out var customerId, out var driverId))
            return Unauthorized(new { success = false, message = "Invalid driver token." });

        var data = await _bootstrapService.GetAsync(customerId, driverId, cancellationToken).ConfigureAwait(false);

        return Ok(new
        {
            success = true,
            activeTasks = data.ActiveTasks,
            locations = data.Locations,
            readerWays = data.ReaderWays,
            syncPolicy = data.SyncPolicy,
        });
    }
}
