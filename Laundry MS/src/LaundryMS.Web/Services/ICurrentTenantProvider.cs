namespace LaundryMS.Web.Services;

public interface ICurrentTenantProvider
{
    ulong? GetCurrentCustomerId();
}
