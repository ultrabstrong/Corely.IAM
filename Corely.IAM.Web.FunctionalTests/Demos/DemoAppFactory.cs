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

public sealed class DemoAppFactory<TNotesContext> : WebApplicationFactory<TNotesContext>
    where TNotesContext : DbContext
{
    private readonly SqliteConnection _iamConnection = OpenInMemory();
    private readonly SqliteConnection _notesConnection = OpenInMemory();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAllKeyed<IEFConfiguration>(EFConfigurationKeys.IAM);
            services.AddKeyedScoped<IEFConfiguration>(
                EFConfigurationKeys.IAM,
                (_, _) => new SqliteEFConfiguration(_iamConnection)
            );

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
