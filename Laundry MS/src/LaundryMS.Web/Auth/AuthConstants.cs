namespace LaundryMS.Web.Auth;

public static class AuthConstants
{
    public const string AccessTokenCookieName = "Demo-ClinoTag-Access-Token";
    public const string UserRoleCookieName = "userRole";

    /// <summary>JWT claim for tenant / customer id (stringified ulong).</summary>
    public const string CustomerIdClaimType = "CustomerId";

    /// <summary>JWT claim for driver id (stringified ulong) when role is DRIVER.</summary>
    public const string DriverIdClaimType = "DriverId";

    /// <summary>JWT claim for table reader id (stringified ulong) when role is TABLE.</summary>
    public const string ReaderIdClaimType = "ReaderId";

    public const string TableRole = "TABLE";
}
