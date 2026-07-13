using EternalX.Blazor.Server.Api;
using EternalX.Blazor.Server.Auth;
using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Server.Services;
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
builder.Services.AddSingleton(new AutoReplyOptions());

// Background Auto-Reply Service (policy-gated: quiet period + reply cap)
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

// Authentication: EternalSocial gateway only (no local OIDC). The proxy supplies
// X-Gateway-Key (shared secret) plus X-Auth-UserId/Name/Email on each request.
if (string.IsNullOrWhiteSpace(builder.Configuration["GATEWAY_KEY"]))
{
    throw new InvalidOperationException(
        "GATEWAY_KEY is required. EternalX authenticates only via the EternalSocial proxy (X-Gateway-Key + X-Auth-* headers).");
}

builder.Services.AddAuthentication(GatewayAuthHandler.SchemeName)
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, GatewayAuthHandler>(
        GatewayAuthHandler.SchemeName, _ => { });

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