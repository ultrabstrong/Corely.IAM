using Corely.DataAccess.Interfaces.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Extensions;

internal static class ServiceScopeFactoryExtensions
{
    extension(IServiceScopeFactory scopeFactory)
    {
        public async Task CompleteDefaultsAtStartupAsync(
            ILogger logger,
            string defaults,
            Func<IServiceProvider, Task> complete
        )
        {
            try
            {
                await InUnitOfWorkAsync(scopeFactory, complete);
            }
            catch (Exception ex)
            {
                logger.LogInformation(
                    ex,
                    "Could not complete {Defaults} on the first try, as when another instance completes them at the same time; trying again",
                    defaults
                );
                try
                {
                    await InUnitOfWorkAsync(scopeFactory, complete);
                }
                catch (Exception retryEx)
                {
                    logger.LogError(
                        retryEx,
                        "Could not complete {Defaults} at startup; it is retried on the next start",
                        defaults
                    );
                }
            }
        }
    }

    private static async Task InUnitOfWorkAsync(
        IServiceScopeFactory scopeFactory,
        Func<IServiceProvider, Task> complete
    )
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkProvider>();
        await unitOfWork.BeginAsync();
        try
        {
            await complete(scope.ServiceProvider);
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
        await unitOfWork.CommitAsync();
    }
}
