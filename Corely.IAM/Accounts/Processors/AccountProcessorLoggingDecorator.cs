using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Extensions;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Accounts.Processors;

internal class AccountProcessorLoggingDecorator(
    IAccountProcessor inner,
    ILogger<AccountProcessorLoggingDecorator> logger
) : IAccountProcessor
{
    private readonly IAccountProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<AccountProcessorLoggingDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

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

    public async Task<List<Account>> GetAccountsForUserAsync(int userId) =>
        await _logger.ExecuteWithLogging(
            nameof(AccountProcessor),
            userId,
            () => _inner.GetAccountsForUserAsync(userId),
            logResult: false
        );

    public async Task<DeleteAccountResult> DeleteAccountAsync(int accountId) =>
        await _logger.ExecuteWithLogging(
            nameof(AccountProcessor),
            accountId,
            () => _inner.DeleteAccountAsync(accountId),
            logResult: true
        );

    public async Task<AddUserToAccountResult> AddUserToAccountAsync(
        AddUserToAccountRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(AccountProcessor),
            request,
            () => _inner.AddUserToAccountAsync(request),
            logResult: true
        );

    public async Task<RemoveUserFromAccountResult> RemoveUserFromAccountAsync(
        RemoveUserFromAccountRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(AccountProcessor),
            request,
            () => _inner.RemoveUserFromAccountAsync(request),
            logResult: true
        );
}
