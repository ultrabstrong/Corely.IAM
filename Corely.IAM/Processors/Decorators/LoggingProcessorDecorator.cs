using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Extensions;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Processors.Decorators;

/// <summary>
/// Decorator that logs method entry, exit, and exceptions for account processor operations.
/// </summary>
internal class LoggingAccountProcessorDecorator : IAccountProcessor
{
    private readonly IAccountProcessor _inner;
    private readonly ILogger<LoggingAccountProcessorDecorator> _logger;

    public LoggingAccountProcessorDecorator(
        IAccountProcessor inner,
        ILogger<LoggingAccountProcessorDecorator> logger
    )
    {
        _inner = inner.ThrowIfNull(nameof(inner));
        _logger = logger.ThrowIfNull(nameof(logger));
    }

    public async Task<CreateAccountResult> CreateAccountAsync(CreateAccountRequest request) =>
        await _logger.ExecuteWithLogging(
            nameof(AccountProcessor),
            request,
            () => _inner.CreateAccountAsync(request),
            logResult: true
        );

    public async Task<Account?> GetAccountAsync(int accountId) =>
        await _logger.ExecuteWithLogging(
            nameof(AccountProcessor),
            accountId,
            () => _inner.GetAccountAsync(accountId),
            logResult: false
        );

    public async Task<Account?> GetAccountAsync(string accountName) =>
        await _logger.ExecuteWithLogging(
            nameof(AccountProcessor),
            accountName,
            () => _inner.GetAccountAsync(accountName),
            logResult: true
        );
}
