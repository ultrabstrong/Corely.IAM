using System.Security.Cryptography;
using Corely.DataAccess.EntityFramework.Configurations;
using Corely.IAM.DataAccess;
using Corely.Security.Hashing;
using Corely.Security.Hashing.Factories;
using Corely.Security.Hashing.Providers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Web.FunctionalTests.Infrastructure;

public sealed class IamWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public static readonly Uri BaseAddress = new("https://localhost");

    public TestTimeProvider TimeProvider { get; } =
        new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

    public int AuthTokenTtlSeconds { get; init; } = 3600;
    public int AuthSessionTtlSeconds { get; init; } = 604800;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        foreach (
            var (key, value) in new Dictionary<string, string>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
                ["Database:Provider"] = "mssql",
                ["Security:SystemKey"] = CreateSystemKey(),
                ["SecurityOptions:AuthTokenTtlSeconds"] = AuthTokenTtlSeconds.ToString(),
                ["SecurityOptions:AuthSessionTtlSeconds"] = AuthSessionTtlSeconds.ToString(),
                ["SecurityOptions:MaxLoginAttempts"] = "5",
                ["SecurityOptions:MfaChallengeTimeoutSeconds"] = "300",
                ["SecurityOptions:GoogleClientId"] = "",
                ["DemoFeatures:EnablePasswordRecoveryPreview"] = "true",
            }
        )
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEFConfiguration>();
            services.AddSingleton<IEFConfiguration>(_ => new SqliteEFConfiguration(_connection));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(TimeProvider);

            services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        });
    }

    private static string CreateSystemKey()
    {
        using var aes = Aes.Create();
        aes.GenerateKey();
        return Convert.ToBase64String(aes.Key);
    }

    public async Task InitializeDatabaseAsync()
    {
        await _connection.OpenAsync();

        Services
            .GetRequiredService<IHashProviderFactory>()
            .UpdateProvider(HashConstants.PBKDF2_SHA256_CODE, new Pbkdf2HashProvider(1000));

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IamDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public HttpClient CreateTestClient() =>
        CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = BaseAddress,
                HandleCookies = true,
            }
        );

    public async Task<T> WithScopeAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        using var scope = Services.CreateScope();
        return await work(scope.ServiceProvider);
    }

    public async Task WithScopeAsync(Func<IServiceProvider, Task> work)
    {
        using var scope = Services.CreateScope();
        await work(scope.ServiceProvider);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }
        base.Dispose(disposing);
    }
}
