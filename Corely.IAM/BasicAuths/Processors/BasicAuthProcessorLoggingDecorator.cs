using Corely.Common.Extensions;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.Extensions;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.BasicAuths.Processors;

internal class BasicAuthProcessorLoggingDecorator(
    IBasicAuthProcessor inner,
    ILogger<BasicAuthProcessorLoggingDecorator> logger
) : IBasicAuthProcessor
{
    private readonly IBasicAuthProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<BasicAuthProcessorLoggingDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

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
