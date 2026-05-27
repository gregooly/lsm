using System.Security.Claims;

namespace LaundryMS.Web.Auth;

public static class TableClaimsExtensions
{
    /// <summary>Table app JWT uses the same <see cref="AuthConstants.DriverIdClaimType"/> as device-login (drivers table).</summary>
    public static bool TryGetTableCustomerAndDriverId(this ClaimsPrincipal user, out ulong customerId, out ulong driverId)
    {
        customerId = 0;
        driverId = 0;
        var c = user.FindFirst(AuthConstants.CustomerIdClaimType)?.Value;
        var d = user.FindFirst(AuthConstants.DriverIdClaimType)?.Value;
        return ulong.TryParse(c, out customerId)
               && ulong.TryParse(d, out driverId)
               && customerId != 0
               && driverId != 0;
    }
}
