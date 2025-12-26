using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Extensions;

internal static class LoggerExtensions
{
    public static async Task<TResult> ExecuteWithLoggingAsync<TRequest, TResult>(
        this ILogger logger,
        string className,
        TRequest request,
        Func<Task<TResult>> operation,
        bool logResult = false,
        [CallerMemberName] string methodName = ""
    )
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            logger.LogTrace(
                "[{Class}] {Method} starting with request {@Request}",
                className,
                methodName,
                request
            );

            var stopwatch = Stopwatch.StartNew();
            var result = await operation();
            stopwatch.Stop();

            if (logResult)
            {
                logger.LogTrace(
                    "[{Class}] {Method} completed in {ElapsedMs} ms with result {@Result}",
                    className,
                    methodName,
                    stopwatch.ElapsedMilliseconds,
                    result
                );
            }
            else
            {
                logger.LogTrace(
                    "[{Class}] {Method} completed in {ElapsedMs}ms",
                    className,
                    methodName,
                    stopwatch.ElapsedMilliseconds
                );
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{Class}] {Method} failed", className, methodName);
            throw;
        }
    }

    public static async Task<TResult> ExecuteWithLoggingAsync<TResult>(
        this ILogger logger,
        string className,
        Func<Task<TResult>> operation,
        bool logResult = false,
        [CallerMemberName] string methodName = ""
    )
    {
        try
        {
            logger.LogTrace("[{Class}] {Method} starting", className, methodName);

            var stopwatch = Stopwatch.StartNew();
            var result = await operation();
            stopwatch.Stop();

            if (logResult)
            {
                logger.LogTrace(
                    "[{Class}] {Method} completed in {ElapsedMs} ms with result {@Result}",
                    className,
                    methodName,
                    stopwatch.ElapsedMilliseconds,
                    result
                );
            }
            else
            {
                logger.LogTrace(
                    "[{Class}] {Method} completed in {ElapsedMs}ms",
                    className,
                    methodName,
                    stopwatch.ElapsedMilliseconds
                );
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{Class}] {Method} failed", className, methodName);
            throw;
        }
    }

    public static async Task ExecuteWithLoggingAsync<TRequest>(
        this ILogger logger,
        string className,
        TRequest request,
        Func<Task> operation,
        [CallerMemberName] string methodName = ""
    )
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            logger.LogTrace(
                "[{Class}] {Method} starting with request {@Request}",
                className,
                methodName,
                request
            );

            var stopwatch = Stopwatch.StartNew();
            await operation();
            stopwatch.Stop();

            logger.LogTrace(
                "[{Class}] {Method} completed in {ElapsedMs} ms",
                className,
                methodName,
                stopwatch.ElapsedMilliseconds
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{Class}] {Method} failed", className, methodName);
            throw;
        }
    }

    public static async Task ExecuteWithLoggingAsync(
        this ILogger logger,
        string className,
        Func<Task> operation,
        [CallerMemberName] string methodName = ""
    )
    {
        try
        {
            logger.LogTrace("[{Class}] {Method} starting", className, methodName);

            var stopwatch = Stopwatch.StartNew();
            await operation();
            stopwatch.Stop();

            logger.LogTrace(
                "[{Class}] {Method} completed in {ElapsedMs} ms",
                className,
                methodName,
                stopwatch.ElapsedMilliseconds
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{Class}] {Method} failed", className, methodName);
            throw;
        }
    }
}
