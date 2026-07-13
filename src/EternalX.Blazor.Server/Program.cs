using EternalX.Blazor.Server.Api;
using EternalX.Blazor.Server.Auth;
using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Server.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks();

// LiteDB
builder.Services.AddSingleton<LiteDbService>();

// AI and Moderator
builder.Services.AddSingleton<AiService>();
builder.Services.AddSingleton<ModeratorService>();

// Background Auto-Reply Service
builder.Services.AddHostedService<AutoReplyBackgroundService>();

// Rate Limiting: 1 post per minute per IP. Scoped to the posting endpoint as the
// "post" policy (a global limiter would throttle page assets and feed reads too).
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("post", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 1,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please wait before posting again.");
    };
});

// Authentication. Behind the EternalSocial gateway (GATEWAY_KEY set) identity
// arrives as forwarded X-Auth-* headers and the gateway scheme is used; the
// original OIDC registration below remains for standalone use.
var gatewayMode = !string.IsNullOrWhiteSpace(builder.Configuration["GATEWAY_KEY"]);
if (gatewayMode)
{
    builder.Services.AddAuthentication(GatewayAuthHandler.SchemeName)
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, GatewayAuthHandler>(GatewayAuthHandler.SchemeName, _ => { });
}
else
{

// Authentication (OpenID Connect - Google, Microsoft, GitHub)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "oidc";
})
.AddCookie("Cookies")
.AddOpenIdConnect("Google", options =>
{
    options.Authority = "https://accounts.google.com";
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.ResponseType = "code";
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.SaveTokens = true;
})
.AddOpenIdConnect("Microsoft", options =>
{
    options.Authority = "https://login.microsoftonline.com/common/v2.0";
    options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
    options.ResponseType = "code";
})
.AddOpenIdConnect("GitHub", options =>
{
    options.Authority = "https://github.com/login/oauth";
    options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];
});

} // end standalone authentication registration

builder.Services.AddAuthorization();

var app = builder.Build();

// Behind the gateway: honor its forwarded headers and absorb the /x path prefix.
var forwarded = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
};
forwarded.KnownIPNetworks.Clear();
forwarded.KnownProxies.Clear();
app.UseForwardedHeaders(forwarded);

var pathBase = (builder.Configuration["PATH_BASE"] ?? "").TrimEnd('/');
if (!string.IsNullOrEmpty(pathBase))
    app.UsePathBase(pathBase);

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapPostEndpoints();

// SPA fallback with the base href rewritten for the gateway prefix.
string? LoadIndex()
{
    var file = app.Environment.WebRootFileProvider.GetFileInfo("index.html");
    if (!file.Exists) return null;
    using var reader = new StreamReader(file.CreateReadStream());
    var html = reader.ReadToEnd();
    var prefix = string.IsNullOrEmpty(pathBase) ? "" : pathBase;
    return html.Replace("<base href=\"/\" />", $"<base href=\"{prefix}/\" />");
}
var index = new Lazy<string?>(LoadIndex);
app.MapFallback(async ctx =>
{
    if (index.Value is null) { ctx.Response.StatusCode = StatusCodes.Status404NotFound; return; }
    ctx.Response.ContentType = "text/html; charset=utf-8";
    ctx.Response.Headers.CacheControl = "no-cache";
    await ctx.Response.WriteAsync(index.Value);
});

app.Run();

/// <summary>Exposed for integration testing.</summary>
public partial class Program;