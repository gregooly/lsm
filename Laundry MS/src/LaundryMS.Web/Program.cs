using System.Text;
using LaundryMS.Web.Auth;
using LaundryMS.Web.Data;
using LaundryMS.Web.Options;
using LaundryMS.Web.Services;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Configuration Jwt:SigningKey is required (min 32 UTF-8 bytes for HS256).");
if (Encoding.UTF8.GetBytes(jwtSigningKey).Length < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 UTF-8 bytes.");
}

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ICurrentTenantProvider, HttpContextCurrentTenantProvider>();
builder.Services.AddScoped<ILinenEmailReportService, LinenEmailReportService>();
builder.Services.AddScoped<DriverLinenIngestService>();
builder.Services.AddScoped<IDriverBootstrapService, DriverBootstrapService>();
builder.Services.AddScoped<ITableBootstrapService, TableBootstrapService>();
builder.Services.AddScoped<TableLinenIngestService>();

builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection(MqttOptions.SectionName));
builder.Services.AddSingleton<IMqttReaderStateRegistry, MqttReaderStateRegistry>();
builder.Services.AddScoped<IFixedReaderIngestService, FixedReaderIngestService>();
builder.Services.AddHostedService<MqttIngestHostedService>();

builder.Services.AddControllersWithViews(options =>
{
    var requireAuthenticated = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(requireAuthenticated));
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.SaveToken = true;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "LaundryMS",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "LaundryMS",
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue(AuthConstants.AccessTokenCookieName, out var token)
                    && !string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }

                if (string.IsNullOrEmpty(context.Token)
                    && context.Request.Headers.TryGetValue(HeaderNames.Authorization, out var authHeader))
                {
                    var v = authHeader.ToString();
                    const string prefix = "Bearer ";
                    if (v.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = v[prefix.Length..].Trim();
                    }
                }

                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json; charset=utf-8";
                    var message = string.IsNullOrEmpty(context.ErrorDescription)
                        ? "Unauthorized. Send Authorization: Bearer <token> from POST /api/auth/device-login."
                        : context.ErrorDescription;
                    await context.Response.WriteAsJsonAsync(new { success = false, message }).ConfigureAwait(false);
                    return;
                }

                var returnPath = (context.Request.PathBase + context.Request.Path + context.Request.QueryString).ToString();
                if (string.IsNullOrEmpty(returnPath))
                {
                    returnPath = "/";
                }

                var returnUrl = Uri.EscapeDataString(returnPath);
                context.Response.Redirect($"/Account/Login?returnUrl={returnUrl}");
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<LaundryMsDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("LaundryMs");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Connection string 'LaundryMs' is not configured.");
    }

    var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
    options.UseMySql(connectionString, serverVersion, mySql =>
    {
        mySql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Dashboard}/{id?}")
    .WithStaticAssets();

app.Run();
