using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM;
using Corely.IAM.Web.Extensions;
using Corely.IAM.WebApp;
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
});
builder.Services.AddIAMWebBlazor();

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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// The app's Content-Security-Policy is the app's to set; Corely.IAM.Web sets none. This allows
// Blazor Server (inline bootstrap script, SignalR over WebSockets) and nothing external. An app
// using Google sign-in adds Google's sources - see Corely.IAM.Web/Docs/security.md.
app.Use(
    async (context, next) =>
    {
        context.Response.Headers.ContentSecurityPolicy =
            "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; "
            + "connect-src 'self' wss: ws:; img-src 'self' data:; font-src 'self'; "
            + "frame-ancestors 'none'; form-action 'self'; base-uri 'self'; object-src 'none'";
        await next();
    }
);
app.UseIAMWebAuthentication();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddAdditionalAssemblies(typeof(Corely.IAM.Web.AppRoutes).Assembly)
    .AddInteractiveServerRenderMode();

app.Run();

// Exposed so Corely.IAM.Web.FunctionalTests can boot this host with WebApplicationFactory.
public partial class Program { }
