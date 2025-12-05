using Corely.Common.Extensions;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Processors;

internal abstract class ProcessorBase
{
    protected readonly ILogger Logger;

    protected ProcessorBase(ILogger logger)
    {
        Logger = logger.ThrowIfNull(nameof(logger));
    }

    protected async Task<TResult> LogRequestResultAspect<TRequest, TResult>(
        string className,
        string methodName,
        TRequest request,
        Func<Task<TResult>> next
    )
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            Logger.LogDebug(
                "[{Class}] {Method} starting with request {@Request}",
                className,
                methodName,
                request
            );
            var result = await next();
            Logger.LogDebug(
                "[{Class}] {Method} completed with result {@Result}",
                className,
                methodName,
                result
            );
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{Class}] {Method} failed", className, methodName);
            throw;
        }
    }

    protected async Task<TResult> LogRequestAspect<TRequest, TResult>(
        string className,
        string methodName,
        TRequest request,
        Func<Task<TResult>> next
    )
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            Logger.LogDebug(
                "[{Class}] {Method} starting with request {@Request}",
                className,
                methodName,
                request
            );
            var result = await next();
            Logger.LogDebug("[{Class}] {Method} completed with result", className, methodName);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{Class}] {Method} failed", className, methodName);
            throw;
        }
    }

    protected async Task LogRequestAspect<TRequest>(
        string className,
        string methodName,
        TRequest request,
        Func<Task> next
    )
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            Logger.LogDebug(
                "[{Class}] {Method} starting with request {@Request}",
                className,
                methodName,
                request
            );
            await next();
            Logger.LogDebug("[{Class}] {Method} completed with result", className, methodName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{Class}] {Method} failed", className, methodName);
            throw;
        }
    }

    protected async Task<TResult> LogAspect<TResult>(
        string className,
        string methodName,
        Func<Task<TResult>> next
    )
    {
        try
        {
            Logger.LogDebug("[{Class}] {Method} starting", className, methodName);
            var result = await next();
            Logger.LogDebug("[{Class}] {Method} completed", className, methodName);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{Class}] {Method} failed", className, methodName);
            throw;
        }
    }

    protected async Task LogAspect(string className, string methodName, Func<Task> next)
    {
        try
        {
            Logger.LogDebug("[{Class}] {Method} starting", className, methodName);
            await next();
            Logger.LogDebug("[{Class}] {Method} completed", className, methodName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{Class}] {Method} failed", className, methodName);
            throw;
        }
    }
}
