using Corely.IAM.Permissions.Processors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Permissions;

internal sealed class OwnerDefaultsStartup(
    IServiceScopeFactory scopeFactory,
    ILogger<OwnerDefaultsStartup> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope
                .ServiceProvider.GetRequiredService<IPermissionProcessor>()
                .CreateMissingOwnerDefaultsAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Could not give existing Owner roles their missing owner defaults at startup; it is retried on the next start"
            );
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
