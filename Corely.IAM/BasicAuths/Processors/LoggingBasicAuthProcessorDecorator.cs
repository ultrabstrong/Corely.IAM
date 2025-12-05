using Corely.Common.Extensions;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.Extensions;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.BasicAuths.Processors;

internal class LoggingBasicAuthProcessorDecorator : IBasicAuthProcessor
{
    private readonly IBasicAuthProcessor _inner;
    private readonly ILogger<LoggingBasicAuthProcessorDecorator> _logger;

    public LoggingBasicAuthProcessorDecorator(
        IBasicAuthProcessor inner,
        ILogger<LoggingBasicAuthProcessorDecorator> logger
    )
    {
        _inner = inner.ThrowIfNull(nameof(inner));
        _logger = logger.ThrowIfNull(nameof(logger));
    }

    public async Task<UpsertBasicAuthResult> UpsertBasicAuthAsync(UpsertBasicAuthRequest request) =>
        await _logger.ExecuteWithLogging(
            nameof(BasicAuthProcessor),
            request,
            () => _inner.UpsertBasicAuthAsync(request),
            logResult: true
        );

    public async Task<bool> VerifyBasicAuthAsync(VerifyBasicAuthRequest request) =>
        await _logger.ExecuteWithLogging(
            nameof(BasicAuthProcessor),
            request,
            () => _inner.VerifyBasicAuthAsync(request),
            logResult: true
        );
}
