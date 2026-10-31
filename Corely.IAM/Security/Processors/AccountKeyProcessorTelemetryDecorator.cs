using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Security.Processors;

internal class AccountKeyProcessorTelemetryDecorator(
    IAccountKeyProcessor inner,
    ILogger<AccountKeyProcessorTelemetryDecorator> logger
) : IAccountKeyProcessor
{
    private readonly IAccountKeyProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<AccountKeyProcessorTelemetryDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<GetResult<AccountJsonWebKeySet>> GetJsonWebKeySetAsync(Guid accountId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AccountKeyProcessor),
            new { accountId },
            () => _inner.GetJsonWebKeySetAsync(accountId)
        );

    public async Task<RevealAccountKeyResult> RevealKeyAsync(RevealAccountKeyRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AccountKeyProcessor),
            new { request.AccountId, request.KeyType },
            () => _inner.RevealKeyAsync(request)
        );
}
