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

// A factory, not a scoped context: a Blazor Server circuit is one long-lived scope.
builder.Services.AddDbContextFactory<NotesDbContext>(options =>
    options.UseSqlServer(RequiredConnectionString(builder.Configuration, "Notes"))
);

var app = builder.Build();

// The app's own tables only. IAM's schema is created by the corely-iam-db tool, never at startup.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<NotesDbContext>().Database.EnsureCreated();
}

app.UseIAMWebAuthentication();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorPages();

// The Corely.IAM.Web assembly is deliberately not added here: that would route its admin pages
// (/users, /groups, /roles, /permissions). The sign-in pages are Razor Pages and come regardless.
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

static string RequiredConnectionString(IConfiguration configuration, string name) =>
    configuration.GetConnectionString(name)
    ?? throw new InvalidOperationException($"ConnectionStrings:{name} is not configured");
