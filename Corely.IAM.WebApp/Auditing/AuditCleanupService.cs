using Corely.IAM.Services;

namespace Corely.IAM.WebApp.Auditing;

internal sealed class AuditCleanupService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<AuditCleanupService> logger
) : BackgroundService
{
    private const string DEVICE_ID = "audit-cleanup";
    private static readonly TimeSpan _interval = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval, timeProvider);
        do
        {
            await DeleteExpiredEntriesAsync();
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task DeleteExpiredEntriesAsync()
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            scope
                .ServiceProvider.GetRequiredService<IAuthenticationService>()
                .AuthenticateAsSystem(DEVICE_ID);
            var result = await scope
                .ServiceProvider.GetRequiredService<IAuditService>()
                .DeleteExpiredEntriesAsync();
            logger.LogInformation(
                "Audit cleanup finished with {ResultCode}, deleting {Count} entries",
                result.ResultCode,
                result.DeletedCount
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audit cleanup failed; it runs again on the next tick");
        }
    }
}
