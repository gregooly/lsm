using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LaundryMS.Web.Auth;
using Microsoft.IdentityModel.Tokens;

namespace LaundryMS.Web.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public JwtSecurityToken CreateAccessToken(string userId, string email, string displayName, string role, ulong customerId)
    {
        var issuer = _configuration["Jwt:Issuer"] ?? "LaundryMS";
        var audience = _configuration["Jwt:Audience"] ?? "LaundryMS";
        var signingKey = _configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Configuration Jwt:SigningKey is required (min 32 characters for HS256).");

        var keyBytes = Encoding.UTF8.GetBytes(signingKey);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes (UTF-8) for HS256.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Name, displayName),
            new(ClaimTypes.Role, role),
            new(AuthConstants.CustomerIdClaimType, customerId.ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256);

        var expiresDays = _configuration.GetValue("Jwt:ExpiresDays", 7);

        return new JwtSecurityToken(
            issuer,
            audience,
            claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddDays(expiresDays),
            signingCredentials: credentials);
    }

    public JwtSecurityToken CreateDriverAccessToken(ulong driverId, string displayName, ulong customerId)
    {
        var issuer = _configuration["Jwt:Issuer"] ?? "LaundryMS";
        var audience = _configuration["Jwt:Audience"] ?? "LaundryMS";
        var signingKey = _configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Configuration Jwt:SigningKey is required (min 32 characters for HS256).");

        var keyBytes = Encoding.UTF8.GetBytes(signingKey);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes (UTF-8) for HS256.");
        }

        const string role = "DRIVER";
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, driverId.ToString()),
            new(JwtRegisteredClaimNames.Email, $"driver-{driverId}@device.local"),
            new(JwtRegisteredClaimNames.Name, displayName),
            new(ClaimTypes.Role, role),
            new(AuthConstants.CustomerIdClaimType, customerId.ToString()),
            new(AuthConstants.DriverIdClaimType, driverId.ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256);

        var expiresDays = _configuration.GetValue("Jwt:ExpiresDays", 7);

        return new JwtSecurityToken(
            issuer,
            audience,
            claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddDays(expiresDays),
            signingCredentials: credentials);
    }

    public JwtSecurityToken CreateTableAccessToken(ulong driverId, string displayName, ulong customerId)
    {
        var issuer = _configuration["Jwt:Issuer"] ?? "LaundryMS";
        var audience = _configuration["Jwt:Audience"] ?? "LaundryMS";
        var signingKey = _configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Configuration Jwt:SigningKey is required (min 32 characters for HS256).");

        var keyBytes = Encoding.UTF8.GetBytes(signingKey);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes (UTF-8) for HS256.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, driverId.ToString()),
            new(JwtRegisteredClaimNames.Email, $"table-{driverId}@device.local"),
            new(JwtRegisteredClaimNames.Name, displayName),
            new(ClaimTypes.Role, AuthConstants.TableRole),
            new(AuthConstants.CustomerIdClaimType, customerId.ToString()),
            new(AuthConstants.DriverIdClaimType, driverId.ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256);

        var expiresDays = _configuration.GetValue("Jwt:ExpiresDays", 7);

        return new JwtSecurityToken(
            issuer,
            audience,
            claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddDays(expiresDays),
            signingCredentials: credentials);
    }

    public string WriteToken(JwtSecurityToken token)
    {
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
