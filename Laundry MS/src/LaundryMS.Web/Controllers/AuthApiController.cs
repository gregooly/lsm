using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LaundryMS.Web.Auth;
using LaundryMS.Web.Data;
using LaundryMS.Web.Models.Api;
using LaundryMS.Web.Models.Auth;
using LaundryMS.Web.Models.Entities;
using LaundryMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaundryMS.Web.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private const string PulsePointSignInUrl = "https://api.pulsepoint.clinotag.com/api/user/project/signin";
    private const string PulsePointAllUsersUrl = "https://api.pulsepoint.clinotag.com/api/user/allusers";

    private readonly ILogger<AuthApiController> _logger;
    private readonly LaundryMsDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDriverBootstrapService _driverBootstrapService;
    private readonly ITableBootstrapService _tableBootstrapService;
    private readonly IWebHostEnvironment _environment;

    public AuthApiController(
        ILogger<AuthApiController> logger,
        LaundryMsDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IJwtTokenService jwtTokenService,
        IDriverBootstrapService driverBootstrapService,
        ITableBootstrapService tableBootstrapService,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _db = db;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _jwtTokenService = jwtTokenService;
        _driverBootstrapService = driverBootstrapService;
        _tableBootstrapService = tableBootstrapService;
        _environment = environment;
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid input data" });
            }

            request.Email = request.Email.Trim();
            request.Role = request.Role.Trim();

            if (string.Equals(request.Role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return await HandleAdminLogin(request, cancellationToken).ConfigureAwait(false);
            }

            if (string.Equals(request.Role, "users", StringComparison.OrdinalIgnoreCase))
            {
                return await HandleUsersLogin(request, cancellationToken).ConfigureAwait(false);
            }

            return BadRequest(new { success = false, message = "Invalid role specified" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sign in");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>R2 driver login using handheld device id only.</summary>
    [HttpPost("device-login")]
    public async Task<IActionResult> DeviceLogin([FromBody] DeviceLoginRequest? request, CancellationToken cancellationToken)
    {
        try
        {
            var deviceId = (request?.HandheldDeviceId ?? request?.DeviceId ?? request?.HandheldId ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(deviceId))
            {
                return BadRequest(new { success = false, message = "handheldDeviceId or handheldId is required." });
            }

            var driver = await _db.Drivers.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.IsActive
                         && x.HandheldDeviceId != null
                         && x.HandheldDeviceId.Trim() == deviceId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (driver == null)
            {
                return Unauthorized(new { success = false, message = "Device is not registered or driver is inactive." });
            }

            if (!driver.CustomerId.HasValue || driver.CustomerId.Value == 0)
            {
                return Unauthorized(new { success = false, message = "Driver has no tenant assignment." });
            }

            var customerId = driver.CustomerId.Value;

            var bootstrap = await _driverBootstrapService
                .GetAsync(customerId, driver.Id, cancellationToken)
                .ConfigureAwait(false);

            var token = _jwtTokenService.CreateDriverAccessToken(driver.Id, driver.DriverName, customerId);
            var tokenString = _jwtTokenService.WriteToken(token);

            return Ok(new
            {
                success = true,
                message = "Login successful",
                customerId,
                driver = new
                {
                    id = driver.Id,
                    name = driver.DriverName,
                    isActive = driver.IsActive,
                    deviceId = driver.HandheldDeviceId
                },
                locations = bootstrap.Locations,
                activeTasks = bootstrap.ActiveTasks,
                readerWays = bootstrap.ReaderWays,
                syncPolicy = bootstrap.SyncPolicy,
                token = tokenString
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Device login failed");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>Table app login using tablet device id (same lookup as R2 device-login: drivers.handheld_device_id).</summary>
    [HttpPost("table-login")]
    public async Task<IActionResult> TableLogin([FromBody] TableLoginRequest? request, CancellationToken cancellationToken)
    {
        try
        {
            var deviceId = (request?.HandheldDeviceId ?? request?.DeviceId ?? request?.HandheldId ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(deviceId))
            {
                return BadRequest(new { success = false, message = "deviceId or handheldId is required." });
            }

            var driver = await _db.Drivers.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.IsActive
                         && x.HandheldDeviceId != null
                         && x.HandheldDeviceId.Trim() == deviceId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (driver == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Device is not registered or driver is inactive. Register the tablet under Drivers with Handheld device id matching this device."
                });
            }

            if (!driver.CustomerId.HasValue || driver.CustomerId.Value == 0)
            {
                return Unauthorized(new { success = false, message = "Driver has no tenant assignment." });
            }

            var customerId = driver.CustomerId.Value;

            var bootstrap = await _tableBootstrapService
                .GetLoginBootstrapAsync(customerId, cancellationToken)
                .ConfigureAwait(false);

            var token = _jwtTokenService.CreateTableAccessToken(driver.Id, driver.DriverName, customerId);
            var tokenString = _jwtTokenService.WriteToken(token);

            return Ok(new
            {
                success = true,
                message = "Login successful",
                customerId,
                token = tokenString,
                driver = new
                {
                    id = driver.Id,
                    name = driver.DriverName,
                    isActive = driver.IsActive,
                    deviceId = driver.HandheldDeviceId
                },
                locations = bootstrap.Locations,
                syncPolicy = bootstrap.SyncPolicy
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Table login failed");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    private async Task<IActionResult> HandleAdminLogin(SignInRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();

            var projectId = _configuration.GetValue("PulsePoint:ProjectId", 25);
            var pulsePointRequest = new
            {
                username = request.Email,
                password = request.Password,
                projectId
            };

            var jsonContent = JsonSerializer.Serialize(pulsePointRequest);
            using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            using var result = await httpClient.PostAsync(PulsePointSignInUrl, content, cancellationToken)
                .ConfigureAwait(false);
            var resultContent = await result.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                using var doc = JsonDocument.Parse(resultContent);
                var root = doc.RootElement;

                if (!TryGetStatus(root, out var status))
                {
                    return Unauthorized(new { success = false, message = "Invalid response from authentication service" });
                }

                if (status == 1)
                {
                    var userDetails = await GetPulsePointUserDetails(request.Email, cancellationToken)
                        .ConfigureAwait(false);

                    if (userDetails == null)
                    {
                        return Unauthorized(new { success = false, message = "Login failed: Please check your subscription status" });
                    }

                    const string role = "ADMIN";
                    var customerId = (ulong)userDetails.Id;
                    var displayName = BuildPulsePointDisplayName(userDetails);

                    var token = _jwtTokenService.CreateAccessToken(
                        userDetails.Id.ToString(),
                        request.Email,
                        displayName,
                        role,
                        customerId);

                    var tokenString = _jwtTokenService.WriteToken(token);
                    SetAuthCookies(tokenString, role);

                    return Ok(new
                    {
                        success = true,
                        message = "Login successful",
                        token = tokenString,
                        user = new
                        {
                            customerId = userDetails.Id,
                            id = userDetails.Id,
                            username = userDetails.Email,
                            email = userDetails.Email,
                            role
                        }
                    });
                }

                if (status == -1)
                {
                    var message = TryGetMessage(root) ?? "Account not found";
                    return Unauthorized(new { success = false, message });
                }

                if (status == 0)
                {
                    var message = TryGetMessage(root) ?? "Incorrect password";
                    return Unauthorized(new { success = false, message = "Incorrect password: " + message });
                }

                return Unauthorized(new { success = false, message = $"Unknown status code: {status}" });
            }
            catch (JsonException)
            {
                return StatusCode(503, new { success = false, message = "Invalid response format from authentication service" });
            }
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, new { success = false, message = "External authentication service unavailable" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Admin authentication failed");
            return StatusCode(500, new { success = false, message = "Admin authentication failed" });
        }
    }

    private async Task<IActionResult> HandleUsersLogin(SignInRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var normalizedEmail = request.Email.Trim();

            var user = await _db.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.Email.ToLower() == normalizedEmail.ToLower() && u.IsActive,
                    cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return Unauthorized(new { success = false, message = "This account is not registered." });
            }

            var isValidPassword = false;

            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                try
                {
                    isValidPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
                }
                catch
                {
                    isValidPassword = user.PasswordHash == request.Password;
                    if (isValidPassword)
                    {
                        var tracked = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken)
                            .ConfigureAwait(false);
                        if (tracked != null)
                        {
                            tracked.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                            tracked.UpdatedAt = DateTime.UtcNow;
                            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
            }

            if (!isValidPassword)
            {
                return Unauthorized(new { success = false, message = "Incorrect password" });
            }

            string userRole;
            if (string.IsNullOrEmpty(user.Role) || !string.Equals(user.Role, "ADMIN", StringComparison.OrdinalIgnoreCase))
            {
                userRole = "MANAGER";
            }
            else
            {
                userRole = "ADMIN";
            }

            var customerId = user.CustomerId;

            var token = _jwtTokenService.CreateAccessToken(
                user.Id.ToString(),
                user.Email,
                user.Name,
                userRole,
                customerId);

            var tokenString = _jwtTokenService.WriteToken(token);
            SetAuthCookies(tokenString, userRole);

            return Ok(new
            {
                success = true,
                message = "Login successful",
                token = tokenString,
                user = new
                {
                    customerId = user.CustomerId,
                    id = user.Id,
                    username = user.Email,
                    email = user.Email,
                    firstName = (string?)null,
                    lastName = (string?)null,
                    role = userRole
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "User authentication failed");
            return StatusCode(500, new { success = false, message = "User authentication failed" });
        }
    }

    private async Task<PulsePointUser?> GetPulsePointUserDetails(string email, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes("admin:admin"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            using var result = await httpClient.GetAsync(PulsePointAllUsersUrl, cancellationToken).ConfigureAwait(false);
            var resultContent = await result.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccessStatusCode)
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(resultContent);
                var root = doc.RootElement;

                IEnumerable<JsonElement>? userElements = null;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    userElements = root.EnumerateArray();
                }
                else if (root.ValueKind == JsonValueKind.Object
                         && root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    userElements = data.EnumerateArray();
                }

                if (userElements == null)
                {
                    return null;
                }

                foreach (var userToken in userElements)
                {
                    var mapped = MapPulsePointUser(userToken);
                    if (mapped != null
                        && !string.IsNullOrEmpty(mapped.Email)
                        && string.Equals(mapped.Email, email, StringComparison.OrdinalIgnoreCase))
                    {
                        return mapped;
                    }
                }
            }
            catch (JsonException)
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "PulsePoint user details lookup failed");
        }

        return null;
    }

    private static PulsePointUser? MapPulsePointUser(JsonElement userToken)
    {
        if (!userToken.TryGetProperty("id", out var idEl) || !userToken.TryGetProperty("email", out var emailEl))
        {
            return null;
        }

        if (!idEl.TryGetInt32(out var id))
        {
            return null;
        }

        var email = emailEl.GetString();
        if (string.IsNullOrEmpty(email))
        {
            return null;
        }

        return new PulsePointUser
        {
            Id = id,
            Company = userToken.TryGetProperty("company", out var c) ? c.GetString() : null,
            HotelName = userToken.TryGetProperty("hotelname", out var h) ? h.GetString() : null,
            FirstName = userToken.TryGetProperty("firstname", out var f) ? f.GetString() : null,
            LastName = userToken.TryGetProperty("lastname", out var l) ? l.GetString() : null,
            PhoneNumber = userToken.TryGetProperty("phonenumber", out var p) ? p.GetString() : null,
            Email = email,
            Address = userToken.TryGetProperty("address", out var a) ? a.GetString() : null,
            Contact = userToken.TryGetProperty("contact", out var ct) ? ct.GetString() : null,
            Status = userToken.TryGetProperty("status", out var st) && st.TryGetInt32(out var stv) ? stv : 0,
            Role = userToken.TryGetProperty("role", out var r) && r.TryGetInt32(out var rv) ? rv : 0,
            IsVerify = userToken.TryGetProperty("isVerify", out var iv) && iv.TryGetInt32(out var isv) ? isv : 0
        };
    }

    private static string BuildPulsePointDisplayName(PulsePointUser u)
    {
        var parts = new[] { u.FirstName, u.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        if (parts.Length > 0)
        {
            return string.Join(" ", parts);
        }

        return u.Email ?? u.Id.ToString();
    }

    private static bool TryGetStatus(JsonElement root, out int status)
    {
        status = 0;
        if (!root.TryGetProperty("status", out var p))
        {
            return false;
        }

        if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out status))
        {
            return true;
        }

        if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out status))
        {
            return true;
        }

        return false;
    }

    private static string? TryGetMessage(JsonElement root)
    {
        if (!root.TryGetProperty("message", out var m))
        {
            return null;
        }

        return m.ValueKind switch
        {
            JsonValueKind.String => m.GetString(),
            JsonValueKind.Number => m.GetRawText(),
            _ => m.ToString()
        };
    }

    private void SetAuthCookies(string token, string role)
    {
        var secure = _environment.IsProduction()
                     || string.Equals(_configuration["Auth:CookieSecure"], "Always", StringComparison.OrdinalIgnoreCase)
                     || HttpContext.Request.IsHttps;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/",
            Secure = secure
        };

        Response.Cookies.Append(AuthConstants.AccessTokenCookieName, token, cookieOptions);

        var roleCookieOptions = new CookieOptions
        {
            HttpOnly = false,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/",
            Secure = secure
        };

        Response.Cookies.Append(AuthConstants.UserRoleCookieName, role, roleCookieOptions);
    }
}
