using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Platform.Entities;
using Corely.IAM.Platform.Models;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Persistence;

public class PlatformSecuritySettingsTests : IAsyncLifetime
{
    private readonly IamScenario _scenario = new();

    public ValueTask InitializeAsync() => _scenario.InitializeAsync();

    public ValueTask DisposeAsync() => _scenario.DisposeAsync();

    private Task<SecurityPolicy> PolicyInForceAsync() =>
        _scenario.Host.WithScopeAsync(services =>
            services.GetRequiredService<ISecurityPolicyProvider>().GetPolicyAsync()
        );

    private Task BootstrapAsync() =>
        _scenario.Host.WithScopeAsync(async services =>
        {
            var result = await services
                .GetRequiredService<IPlatformService>()
                .BootstrapPlatformAsync(
                    new BootstrapPlatformRequest(
                        "Platform",
                        "platform-owner",
                        "platform-owner@example.com"
                    )
                );
            Assert.Equal(BootstrapPlatformResultCode.Success, result.ResultCode);
        });

    private Task<ModifyResult> SaveAsync(SecurityPolicy settings) =>
        _scenario.AsSystemAsync(services =>
            services.GetRequiredService<IPlatformService>().UpdateSecuritySettingsAsync(settings)
        );

    [Fact]
    public async Task WithoutAPlatformAccount_ConfigurationApplies_AndCannotBeSaved()
    {
        var saved = await SaveAsync(SecurityPolicy.Default with { MaxLoginAttempts = 10 });

        Assert.Equal(ModifyResultCode.NotFoundError, saved.ResultCode);
        Assert.Equal(5, (await PolicyInForceAsync()).MaxLoginAttempts);
        Assert.False(
            await _scenario.Host.QueryAsync(db =>
                db.Set<PlatformSecuritySettingsEntity>().AnyAsync()
            )
        );
    }

    [Fact]
    public async Task Bootstrap_SavesTheConfiguredSettings()
    {
        await BootstrapAsync();

        var row = await _scenario.Host.QueryAsync(db =>
            db.Set<PlatformSecuritySettingsEntity>().SingleAsync()
        );
        Assert.Equal(5, row.MaxLoginAttempts);
        Assert.Equal(3600, row.AuthTokenTtlSeconds);
        Assert.Equal(SecurityPolicy.Default.PasswordMinimumLength, row.PasswordMinimumLength);
    }

    [Fact]
    public async Task SavedSettings_RoundTrip_AndOverrideConfiguration()
    {
        await BootstrapAsync();
        var settings = SecurityPolicy.Default with
        {
            PasswordMinimumLength = 12,
            RequireNonAlphanumeric = true,
            MaxLoginAttempts = 7,
            TotpRecoveryCodeCount = 6,
        };

        var saved = await SaveAsync(settings);

        Assert.Equal(ModifyResultCode.Success, saved.ResultCode);
        Assert.Equal(settings, await PolicyInForceAsync());
    }

    [Fact]
    public async Task ASavedLockoutLimit_TakesEffectOnTheNextSignIn()
    {
        await BootstrapAsync();
        await SaveAsync(SecurityPolicy.Default with { MaxLoginAttempts = 3 });

        for (var i = 0; i < 3; i++)
        {
            await _scenario.Host.WithScopeAsync(services =>
                services
                    .GetRequiredService<IAuthenticationService>()
                    .SignInAsync(
                        new SignInRequest(_scenario.DirectMemberUsername, "wrong", "device")
                    )
            );
        }
        var signIn = await _scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IAuthenticationService>()
                .SignInAsync(
                    new SignInRequest(
                        _scenario.DirectMemberUsername,
                        IamScenario.Password,
                        "device"
                    )
                )
        );

        Assert.Equal(SignInResultCode.UserLockedError, signIn.ResultCode);
    }

    [Fact]
    public async Task Bootstrap_IsRefused_BeforeWritingAnything_WhenTheConfiguredSettingsAreInvalid()
    {
        using var host = new IamTestHost(
            configurationOverrides: new Dictionary<string, string?>
            {
                ["SecurityOptions:MaxLoginAttempts"] = "0",
            }
        );

        var result = await host.WithScopeAsync(services =>
            services
                .GetRequiredService<IPlatformService>()
                .BootstrapPlatformAsync(
                    new BootstrapPlatformRequest(
                        "Platform",
                        "platform-owner",
                        "platform-owner@example.com"
                    )
                )
        );

        Assert.Equal(BootstrapPlatformResultCode.InvalidSecuritySettingsError, result.ResultCode);
        Assert.False(await host.QueryAsync(db => db.Users.AnyAsync()));
        Assert.False(await host.QueryAsync(db => db.Accounts.AnyAsync()));
    }

    [Fact]
    public async Task AnInvalidSave_IsRefused_AndChangesNothing()
    {
        await BootstrapAsync();

        var saved = await SaveAsync(SecurityPolicy.Default with { MaxLoginAttempts = 0 });

        Assert.Equal(ModifyResultCode.ValidationError, saved.ResultCode);
        Assert.Equal(5, (await PolicyInForceAsync()).MaxLoginAttempts);
    }
}
