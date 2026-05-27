using LaundryMS.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMS.Web.Controllers;

public abstract class TenantScopedController : Controller
{
    protected ulong? GetCurrentCustomerId()
    {
        var claimValue = User.FindFirst(AuthConstants.CustomerIdClaimType)?.Value;
        return ulong.TryParse(claimValue, out var customerId) ? customerId : null;
    }

    protected ulong GetCurrentCustomerIdRequired()
    {
        var customerId = GetCurrentCustomerId();
        if (!customerId.HasValue || customerId.Value == 0)
            throw new UnauthorizedAccessException("Missing or invalid CustomerId claim.");
        return customerId.Value;
    }

    protected bool TryGetCurrentCustomerId(out ulong customerId)
    {
        var value = GetCurrentCustomerId();
        if (value.HasValue && value.Value != 0)
        {
            customerId = value.Value;
            return true;
        }

        customerId = 0;
        return false;
    }
}
