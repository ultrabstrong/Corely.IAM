using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.DataAccess;
using Corely.IAM.Web.FunctionalTests.Infrastructure;
using Corely.Security.Hashing;
using Corely.Security.Hashing.Factories;
using Corely.Security.Hashing.Providers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Web.FunctionalTests.Demos;

/// <summary>
/// Boots a demo app with both of its databases on in-memory SQLite. The demo's notes context doubles
/// as the entry-point marker: every host's <c>Program</c> shares one global name, so it cannot tell
/// the demos apart.
/// </summary>
public sealed class DemoAppFactory<TNotesContext> : WebApplicationFactory<TNotesContext>
    where TNotesContext : DbContext
{
    // Opened up front: an in-memory SQLite database lasts only while a connection to it is open,
    // and the demo creates its notes tables while the host is still being built.
    private readonly SqliteConnection _iamConnection = OpenInMemory();
    private readonly SqliteConnection _notesConnection = OpenInMemory();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEFConfiguration>();
            services.AddScoped<IEFConfiguration>(_ => new SqliteEFConfiguration(_iamConnection));

            services.RemoveAll<DbContextOptions<TNotesContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TNotesContext>>();
            services.RemoveAll<IDbContextFactory<TNotesContext>>();
            services.AddDbContextFactory<TNotesContext>(o => o.UseSqlite(_notesConnection));

            services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        });
    }

    public void InitializeIamDatabase()
    {
        Services
            .GetRequiredService<IHashProviderFactory>()
            .UpdateProvider(HashConstants.PBKDF2_SHA256_CODE, new Pbkdf2HashProvider(1000));

        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IamDbContext>().Database.EnsureCreated();
    }

    public HttpClient CreateTestClient() =>
        CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = IamWebApplicationFactory.BaseAddress,
                HandleCookies = true,
            }
        );

    private static SqliteConnection OpenInMemory()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _iamConnection.Dispose();
            _notesConnection.Dispose();
        }
        base.Dispose(disposing);
    }
}
