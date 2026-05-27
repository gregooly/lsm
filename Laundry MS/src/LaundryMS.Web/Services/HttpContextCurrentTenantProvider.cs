using LaundryMS.Web.Auth;

namespace LaundryMS.Web.Services;

public class HttpContextCurrentTenantProvider : ICurrentTenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ulong? GetCurrentCustomerId()
    {
        var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirst(AuthConstants.CustomerIdClaimType)?.Value;
        return ulong.TryParse(claimValue, out var customerId) ? customerId : null;
    }
}
