using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Platform.Constants;
using Corely.IAM.Platform.Entities;
using Corely.IAM.Platform.Models;
using Corely.IAM.Validators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Platform;

internal sealed class SecurityPolicyStartup(
    IServiceScopeFactory scopeFactory,
    ConfiguredSecurityPolicy configuredPolicy,
    ILogger<SecurityPolicyStartup> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var validation = scope
            .ServiceProvider.GetRequiredService<IValidationProvider>()
            .Validate(configuredPolicy.Policy);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                $"The security settings in configuration are invalid: {validation.Message}"
            );
        }

        try
        {
            var hasSettingsRow = await scope
                .ServiceProvider.GetRequiredService<IReadonlyRepo<PlatformSecuritySettingsEntity>>()
                .AnyAsync(s => s.Id == PlatformConstants.PLATFORM_SETTINGS_ID);
            if (hasSettingsRow)
            {
                if (configuredPolicy.ConfiguredKeys.Count > 0)
                {
                    logger.LogWarning(
                        "The platform account's security settings are in force, so these configuration keys are ignored: {ConfiguredKeys}. Remove them from configuration",
                        configuredPolicy.ConfiguredKeys
                    );
                }
                return;
            }

            var hasPlatformAccount = await scope
                .ServiceProvider.GetRequiredService<IReadonlyRepo<AccountEntity>>()
                .AnyAsync(a => a.IsPlatformAccount);
            if (hasPlatformAccount)
            {
                logger.LogWarning(
                    "The platform account has no security settings yet, so configuration still applies. Save them on the platform security settings page"
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not check the platform security settings at startup");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
