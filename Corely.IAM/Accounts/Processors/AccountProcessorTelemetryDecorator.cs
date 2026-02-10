using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Extensions;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Accounts.Processors;

internal class AccountProcessorTelemetryDecorator(
    IAccountProcessor inner,
    ILogger<AccountProcessorTelemetryDecorator> logger
) : IAccountProcessor
{
    private readonly IAccountProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<AccountProcessorTelemetryDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<CreateAccountResult> CreateAccountAsync(CreateAccountRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AccountProcessor),
            request,
            () => _inner.CreateAccountAsync(request),
            logResult: true
        );

    public async Task<GetAccountResult> GetAccountAsync(Guid accountId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AccountProcessor),
            accountId,
            () => _inner.GetAccountAsync(accountId),
            logResult: true
        );

    public async Task<DeleteAccountResult> DeleteAccountAsync(Guid accountId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AccountProcessor),
            accountId,
            () => _inner.DeleteAccountAsync(accountId),
            logResult: true
        );

    public async Task<AddUserToAccountResult> AddUserToAccountAsync(
        AddUserToAccountRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AccountProcessor),
            request,
            () => _inner.AddUserToAccountAsync(request),
            logResult: true
        );

    public async Task<RemoveUserFromAccountResult> RemoveUserFromAccountAsync(
        RemoveUserFromAccountRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AccountProcessor),
            request,
            () => _inner.RemoveUserFromAccountAsync(request),
            logResult: true
        );
}
