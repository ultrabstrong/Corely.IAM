using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Web.Extensions;
using Corely.IAM.WebApp;
using Corely.IAM.WebApp.Auditing;
using Corely.IAM.WebApp.Components;
using Corely.IAM.WebApp.DataAccess;
using Corely.IAM.WebApp.Security;
using Microsoft.Extensions.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Corely.IAM.WebApp")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<DemoFeaturesOptions>(
    builder.Configuration.GetSection(DemoFeaturesOptions.NAME)
);
builder.Services.AddIAMWeb(options =>
{
    options.ForgotPasswordPath = WebAppRoutes.ForgotPassword;
    options.PlatformSettingsPages.Add(
        new(
            "Audit",
            WebAppRoutes.PlatformAuditSettings,
            "bi-journal-text",
            PlatformSettings.RESOURCE_TYPE
        )
    );
    options.PlatformSettingsPages.Add(
        new(
            "Security",
            WebAppRoutes.PlatformSecuritySettings,
            "bi-shield-lock",
            SecurityPolicy.RESOURCE_TYPE
        )
    );
});
builder.Services.AddIAMWebBlazor<WebAppLinks>();

var securityConfigProvider = new SecurityConfigurationProvider(builder.Configuration);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection string not found in configuration");

var providerName = builder.Configuration["Database:Provider"] ?? "mssql";
Func<IServiceProvider, IEFConfiguration> efConfig = providerName.ToLowerInvariant() switch
{
    "mysql" => sp => new MySqlEFConfiguration(
        connectionString,
        sp.GetRequiredService<ILoggerFactory>()
    ),
    "mssql" => sp => new MsSqlEFConfiguration(
        connectionString,
        sp.GetRequiredService<ILoggerFactory>()
    ),
    _ => throw new InvalidOperationException($"Unsupported database provider: {providerName}"),
};

var iamOptions = IAMOptions.Create(builder.Configuration, securityConfigProvider, efConfig);
builder.Services.AddIAMServices(iamOptions);
builder.Services.AddHostedService<AuditCleanupService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.Use(
    async (context, next) =>
    {
        var headers = context.Response.Headers;
        headers.XFrameOptions = "DENY";
        headers.XContentTypeOptions = "nosniff";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";
        context.Response.OnStarting(() =>
        {
            if (headers.CacheControl.Count == 0)
            {
                headers.CacheControl = "no-store";
            }
            return Task.CompletedTask;
        });
        headers.ContentSecurityPolicy =
            "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; "
            + "connect-src 'self' wss: ws:; img-src 'self' data:; font-src 'self'; "
            + "frame-ancestors 'none'; form-action 'self'; base-uri 'self'; object-src 'none'";
        await next();
    }
);
app.UseIAMWebAuthentication();
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapRazorComponents<App>().WithStaticAssets().AddInteractiveServerRenderMode();

app.Run();

public partial class Program { }
