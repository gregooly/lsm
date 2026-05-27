using System.IdentityModel.Tokens.Jwt;

namespace LaundryMS.Web.Services;

public interface IJwtTokenService
{
    /// <summary>Creates a signed JWT access token (HS256).</summary>
    JwtSecurityToken CreateAccessToken(string userId, string email, string displayName, string role, ulong customerId);

    /// <summary>Creates a JWT for mobile drivers (R2); role DRIVER, includes DriverId claim.</summary>
    JwtSecurityToken CreateDriverAccessToken(ulong driverId, string displayName, ulong customerId);

    /// <summary>Creates a JWT for the table app; role TABLE, includes DriverId claim (drivers table row).</summary>
    JwtSecurityToken CreateTableAccessToken(ulong driverId, string displayName, ulong customerId);

    string WriteToken(JwtSecurityToken token);
}
