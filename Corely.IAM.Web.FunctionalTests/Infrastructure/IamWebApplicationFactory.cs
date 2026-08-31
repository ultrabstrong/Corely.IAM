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

/// <summary>
/// Boots the real WebApp host in-process against SQLite and a controllable clock.
///
/// This owns the HTTP seam: the genuine <c>AuthenticationTokenMiddleware</c>, Razor Pages,
/// antiforgery, and cookie pipeline all run exactly as they do in production. Only the database
/// provider and the clock are substituted.
/// </summary>
public sealed class IamWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    /// <summary>
    /// Fixed origin so every request is treated as HTTPS. Without this
    /// <c>UseHttpsRedirection</c> would 307 every request, and cookies would be issued
    /// without the Secure flag - which is itself under test.
    /// </summary>
    public static readonly Uri BaseAddress = new("https://localhost");

    public TestTimeProvider TimeProvider { get; } =
        new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

    public int AuthTokenTtlSeconds { get; init; } = 3600;
    public int AuthSessionTtlSeconds { get; init; } = 604800;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration(
            (_, config) =>
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        // Required by Program.cs even though the EF configuration is replaced below.
                        ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
                        ["Database:Provider"] = "mssql",
                        ["Security:SystemKey"] = CreateSystemKey(),
                        ["SecurityOptions:AuthTokenTtlSeconds"] = AuthTokenTtlSeconds.ToString(),
                        ["SecurityOptions:AuthSessionTtlSeconds"] =
                            AuthSessionTtlSeconds.ToString(),
                        ["SecurityOptions:MaxLoginAttempts"] = "5",
                        ["SecurityOptions:MfaChallengeTimeoutSeconds"] = "300",
                        ["SecurityOptions:GoogleClientId"] = "",
                        ["DemoFeatures:EnablePasswordRecoveryPreview"] = "true",
                    }
                )
        );

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEFConfiguration>();
            services.AddSingleton<IEFConfiguration>(_ => new SqliteEFConfiguration(_connection));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(TimeProvider);

            services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        });
    }

    /// <summary>
    /// A throwaway system key per host. Matches the base64 AES key format the library's own
    /// key provider emits; that provider is internal to Corely.Security so it cannot be called
    /// from here.
    /// </summary>
    private static string CreateSystemKey()
    {
        using var aes = Aes.Create();
        aes.GenerateKey();
        return Convert.ToBase64String(aes.Key);
    }

    /// <summary>
    /// Opens the shared connection and creates the schema. Must run before any request, and is
    /// the point at which SQLite's ability to round-trip the IAM entity configurations - the M:M
    /// join entities in particular - is proven.
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        await _connection.OpenAsync();

        // The production PBKDF2 work factor is 600,000 iterations - roughly 200ms per hash. Every
        // test here seeds a user and signs in, so paying that would dominate the run. Behaviour is
        // what is under test; the work factor is asserted in Corely.Security.
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
