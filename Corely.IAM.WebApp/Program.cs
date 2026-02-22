using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM;
using Corely.IAM.Web.Extensions;
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
builder.Services.AddIAMWeb();
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
    "mariadb" => sp => new MySqlEFConfiguration(
        connectionString,
        sp.GetRequiredService<ILoggerFactory>()
    ),
    "mssql" => sp => new MsSqlEFConfiguration(
        connectionString,
        sp.GetRequiredService<ILoggerFactory>()
    ),
    _ => throw new InvalidOperationException($"Unsupported database provider: {providerName}"),
};

builder.Services.AddIAMServicesWithEF(builder.Configuration, securityConfigProvider, efConfig);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseIAMWebAuthentication();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddAdditionalAssemblies(typeof(Corely.IAM.Web.AppRoutes).Assembly)
    .AddInteractiveServerRenderMode();

app.Run();
