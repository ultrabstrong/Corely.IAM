using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Platform;
using Corely.IAM.Platform.Entities;
using Corely.IAM.Platform.Mappers;
using Corely.IAM.Platform.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Platform;

public class SecurityPolicyStartupTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly Mock<ILogger<SecurityPolicyStartup>> _logger = new();

    private SecurityPolicyStartup Startup(ConfiguredSecurityPolicy configured) =>
        new(
            new SharedScopeFactory(_serviceFactory.GetRequiredService<IServiceProvider>()),
            configured,
            _logger.Object
        );

    private int Warnings() =>
        _logger.Invocations.Count(i =>
            i.Method.Name == nameof(ILogger.Log) && (LogLevel)i.Arguments[0] == LogLevel.Warning
        );

    [Fact]
    public async Task Start_IsQuiet_WithoutAPlatformAccount()
    {
        await Startup(new(SecurityPolicy.Default, ["SecurityOptions:MaxLoginAttempts"]))
            .StartAsync(CancellationToken.None);

        Assert.Equal(0, Warnings());
    }

    [Fact]
    public async Task Start_Warns_WhenSavedSettingsMakeConfigurationKeysIgnored()
    {
        await _serviceFactory
            .GetRequiredService<IRepo<PlatformSecuritySettingsEntity>>()
            .CreateAsync(SecurityPolicy.Default.ToEntity());

        await Startup(new(SecurityPolicy.Default, ["SecurityOptions:MaxLoginAttempts"]))
            .StartAsync(CancellationToken.None);

        Assert.Equal(1, Warnings());
    }

    [Fact]
    public async Task Start_IsQuiet_WhenSavedSettingsHaveNoConfigurationKeysBesideThem()
    {
        await _serviceFactory
            .GetRequiredService<IRepo<PlatformSecuritySettingsEntity>>()
            .CreateAsync(SecurityPolicy.Default.ToEntity());

        await Startup(new(SecurityPolicy.Default, [])).StartAsync(CancellationToken.None);

        Assert.Equal(0, Warnings());
    }

    [Fact]
    public async Task Start_Warns_WhenAPlatformAccountHasNoSavedSettings()
    {
        await _serviceFactory
            .GetRequiredService<IRepo<AccountEntity>>()
            .CreateAsync(
                new AccountEntity
                {
                    Id = Guid.CreateVersion7(),
                    AccountName = "Platform",
                    IsPlatformAccount = true,
                }
            );

        await Startup(new(SecurityPolicy.Default, [])).StartAsync(CancellationToken.None);

        Assert.Equal(1, Warnings());
    }

    private sealed class SharedScopeFactory(IServiceProvider provider)
        : IServiceScopeFactory,
            IServiceScope
    {
        public IServiceProvider ServiceProvider => provider;

        public IServiceScope CreateScope() => this;

        public void Dispose() { }
    }
}
