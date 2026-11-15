using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Extensions;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Accounts.Processors;

internal class AccountClosureProcessorTelemetryDecorator(
    IAccountClosureProcessor inner,
    ILogger<AccountClosureProcessorTelemetryDecorator> logger
) : IAccountClosureProcessor
{
    private readonly IAccountClosureProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<AccountClosureProcessorTelemetryDecorator> _logger =
        logger.ThrowIfNull(nameof(logger));

    public async Task<CloseAccountResult> RemoveAccountAccessAsync(Guid accountId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AccountClosureProcessor),
            accountId,
            () => _inner.RemoveAccountAccessAsync(accountId),
            logResult: true
        );

    public async Task<CloseAccountResult> PurgeAccountDataAsync(Guid accountId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AccountClosureProcessor),
            accountId,
            () => _inner.PurgeAccountDataAsync(accountId),
            logResult: true
        );
}
