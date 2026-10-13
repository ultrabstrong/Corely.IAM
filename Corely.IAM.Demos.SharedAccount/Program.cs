using Corely.IAM;
using Corely.IAM.Demos.SharedAccount;
using Corely.IAM.Demos.SharedAccount.Components;
using Corely.IAM.Demos.SharedAccount.Notes;
using Corely.IAM.Web.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddIAMWeb();
builder.Services.AddIAMWebBlazor();

var iamConnection = RequiredConnectionString(builder.Configuration, "Iam");
builder.Services.AddIAMServices(
    IAMOptions.Create(
        builder.Configuration,
        new DemoSecurityConfigurationProvider(builder.Configuration),
        _ => new IamSqlServerConfiguration(iamConnection)
    )
);

builder.Services.AddDbContextFactory<NotesDbContext>(options =>
    options.UseSqlServer(RequiredConnectionString(builder.Configuration, "Notes"))
);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<NotesDbContext>().Database.EnsureCreated();
}

if (args.Contains("--seed"))
{
    await DemoSeed.RunAsync(app.Services);
    return;
}

if (!app.Environment.IsDevelopment())
{
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

static string RequiredConnectionString(IConfiguration configuration, string name) =>
    configuration.GetConnectionString(name)
    ?? throw new InvalidOperationException($"ConnectionStrings:{name} is not configured");
