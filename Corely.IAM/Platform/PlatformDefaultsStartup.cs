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
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope
                .ServiceProvider.GetRequiredService<IPlatformService>()
                .CompletePlatformOwnerPermissionsAsync();
            await scope
                .ServiceProvider.GetRequiredService<PlatformSupportDefaults>()
                .CompleteAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Could not complete the platform account's default roles at startup; it is retried on the next start"
            );
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
