using LaundryMS.Web.Data;
using LaundryMS.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Controllers;

public class MovementEventsController : TenantScopedController
{
    private readonly LaundryMsDbContext _dbContext;

    public MovementEventsController(LaundryMsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentCustomerId(out var customerId))
            return Forbid();

        var events = await _dbContext.LinenMovementEvents
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.OccurredAt)
            .Select(x => new MovementEventListItemViewModel
            {
                Id = x.Id,
                RfidTag = x.LinenItem.RfidTag,
                ReaderName = x.Reader.ReaderName,
                WayName = x.ReaderWay.WayName,
                ProcessingResult = x.ProcessingResult,
                OccurredAt = x.OccurredAt,
                ReceivedAtServer = x.ReceivedAtServer
            })
            .Take(300)
            .ToListAsync(cancellationToken);

        return View(events);
    }
}
