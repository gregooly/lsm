using LaundryMS.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaundryMS.Web.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public AccountController(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    public IActionResult Login(string? returnUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && !Url.IsLocalUrl(returnUrl))
        {
            returnUrl = null;
        }

        ViewData["ReturnUrl"] = string.IsNullOrWhiteSpace(returnUrl) ? null : returnUrl;
        return View();
    }

    public IActionResult Logout()
    {
        var secure = _environment.IsProduction()
                     || string.Equals(_configuration["Auth:CookieSecure"], "Always", StringComparison.OrdinalIgnoreCase)
                     || HttpContext.Request.IsHttps;

        var opts = new CookieOptions
        {
            Path = "/",
            SameSite = SameSiteMode.Lax,
            Secure = secure
        };

        Response.Cookies.Delete(AuthConstants.AccessTokenCookieName, opts);
        Response.Cookies.Delete(AuthConstants.UserRoleCookieName, opts);

        return RedirectToAction(nameof(Login));
    }
}
