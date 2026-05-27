using LaundryMS.Web.Models.Api;

namespace LaundryMS.Web.Services;

public interface ITableBootstrapService
{
    /// <summary>Tenant-wide config after table-login (before Bluetooth reader is linked).</summary>
    Task<TableLoginBootstrapData> GetLoginBootstrapAsync(ulong customerId, CancellationToken cancellationToken);

    /// <summary>Reader-specific scan routes after connection-status returns readerId.</summary>
    Task<TableBootstrapData?> GetAsync(ulong customerId, ulong readerId, CancellationToken cancellationToken);
}
