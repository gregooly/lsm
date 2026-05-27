using LaundryMS.Web.Models.Api;

namespace LaundryMS.Web.Services;

public interface IDriverBootstrapService
{
    Task<DriverBootstrapData> GetAsync(ulong customerId, ulong driverId, CancellationToken cancellationToken);
}
