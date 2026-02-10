using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Extensions;
using Corely.IAM.Filtering;
using Corely.IAM.Filtering.Ordering;
using Corely.IAM.Models;
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

    public async Task<ListResult<Account>> ListAccountsAsync(
        FilterBuilder<Account>? filter,
        OrderBuilder<Account>? order,
        int skip,
        int take
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AccountProcessor),
            () => _inner.ListAccountsAsync(filter, order, skip, take),
            logResult: true
        );

    public async Task<GetResult<Account>> GetAccountByIdAsync(Guid accountId, bool hydrate) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AccountProcessor),
            accountId,
            () => _inner.GetAccountByIdAsync(accountId, hydrate),
            logResult: true
        );
}
