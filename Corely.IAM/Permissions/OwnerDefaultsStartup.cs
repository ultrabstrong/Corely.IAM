using Corely.IAM.Extensions;
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
    public Task StartAsync(CancellationToken cancellationToken) =>
        scopeFactory.CompleteDefaultsAtStartupAsync(
            logger,
            "existing Owner roles' missing owner defaults",
            services =>
                services
                    .GetRequiredService<IPermissionProcessor>()
                    .CreateMissingOwnerDefaultsAsync()
        );

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
