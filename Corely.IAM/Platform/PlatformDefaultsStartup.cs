using Corely.IAM.Extensions;
using Corely.IAM.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Platform;

internal sealed class PlatformDefaultsStartup(
    IServiceScopeFactory scopeFactory,
    ILogger<PlatformDefaultsStartup> logger
) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) =>
        scopeFactory.CompleteDefaultsAtStartupAsync(
            logger,
            "the platform account's default roles",
            async services =>
            {
                await services
                    .GetRequiredService<IPlatformService>()
                    .CompletePlatformOwnerPermissionsAsync();
                await services.GetRequiredService<PlatformSupportDefaults>().CompleteAsync();
            }
        );

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
