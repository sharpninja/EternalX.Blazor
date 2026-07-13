using EternalX.Blazor.Server;
using EternalX.Blazor.Server.Api;
using EternalX.Blazor.Server.Auth;
using EternalX.Blazor.Server.Data;
using EternalX.Blazor.Server.Hubs;
using EternalX.Blazor.Server.Services;
using EternalX.Blazor.Shared;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

// Core app services (explicit AiService factory avoids ambiguous DI constructors).
builder.Services.AddEternalXApplicationServices();
builder.Services.AddSingleton<IFeedNotifier, SignalRFeedNotifier>();
builder.Services.AddHostedService<AutoReplyBackgroundService>();

// Health: process + open LiteDB
builder.Services.AddHealthChecks()
    .AddCheck<LiteDbHealthCheck>("litedb");

// Rate Limiting: 1 post per minute per IP (post policy only).
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

// Authentication: EternalSocial gateway only (no local OIDC).
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
// Non-fingerprinted assets (css/app.css, index.html) must revalidate on every
// load; without this browsers cache heuristically and serve stale styles after
// a redesign (the framework's fingerprinted files under _framework are immune).
var noCacheStatics = new StaticFileOptions
{
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "no-cache"
};
app.UseStaticFiles(noCacheStatics);
app.UseRouting();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapHub<FeedHub>(FeedEvents.HubPath);
app.MapPostEndpoints();
app.MapAdminEndpoints();

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

public partial class Program;
