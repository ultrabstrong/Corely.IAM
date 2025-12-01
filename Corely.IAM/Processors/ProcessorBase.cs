using Corely.Common.Extensions;
using Corely.IAM.Mappers;
using Corely.IAM.Validators;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Processors;

internal abstract class ProcessorBase
{
    private readonly IValidationProvider _validationProvider;
    private readonly IMapProvider _mapProvider;

    protected readonly ILogger Logger;

    protected ProcessorBase(
        IMapProvider mapProvider,
        IValidationProvider validationProvider,
        ILogger logger
    )
    {
        _validationProvider = validationProvider.ThrowIfNull(nameof(validationProvider));
        _mapProvider = mapProvider.ThrowIfNull(nameof(mapProvider));
        Logger = logger.ThrowIfNull(nameof(logger));
    }

    protected T MapThenValidateTo<T>(object? source)
    {
        var mapped = MapTo<T>(source);
        return Validate(mapped);
    }

    protected T? MapTo<T>(object? source)
    {
        try
        {
            return _mapProvider.MapTo<T>(source);
            ;
        }
        catch (Exception ex)
        {
            using var scope = Logger.BeginScope(
                new Dictionary<string, object?> { { "@MapSource", source } }
            );

            Logger.LogWarning(
                ex,
                "Failed to map {MapSourceType} to {MapDestinationType}",
                source?.GetType()?.Name,
                typeof(T)?.Name
            );
            throw;
        }
    }

    protected T Validate<T>(T? model)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(model, nameof(model));
            _validationProvider.ThrowIfInvalid(model);
            return model;
        }
        catch (Exception ex)
        {
            var state = new Dictionary<string, object?>();

            if (
                ex is ValidationException validationException
                && validationException.ValidationResult != null
            )
            {
                state.Add("@ValidationResult", validationException.ValidationResult);
            }

            using var scope = Logger.BeginScope(state);
            Logger.LogWarning(ex, "Validation failed for {ModelType}", model?.GetType()?.Name);
            throw;
        }
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
