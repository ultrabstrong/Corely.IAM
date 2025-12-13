using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class RetrievalService(
    ILogger<RetrievalService> logger,
    IAccountProcessor accountProcessor
) : IRetrievalService
{
    private readonly ILogger<RetrievalService> _logger = logger.ThrowIfNull(nameof(logger));
    private readonly IAccountProcessor _accountProcessor = accountProcessor.ThrowIfNull(
        nameof(accountProcessor)
    );

    public async Task<RetrieveAccountsResult> RetrieveAccountsAsync(RetrieveAccountsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation("Retrieving accounts for user {UserId}", request.UserId);

        var result = await _accountProcessor.ListAccountsForUserAsync(request.UserId);

        if (result.ResultCode != ListAccountsForUserResultCode.Success)
        {
            _logger.LogInformation("Retrieving accounts failed for user {UserId}", request.UserId);
            return new RetrieveAccountsResult(
                (RetrieveAccountsResultCode)result.ResultCode,
                result.Message,
                []
            );
        }

        _logger.LogInformation(
            "Retrieved {AccountCount} accounts for user {UserId}",
            result.Accounts.Count,
            request.UserId
        );

        return new RetrieveAccountsResult(
            RetrieveAccountsResultCode.Success,
            string.Empty,
            result.Accounts
        );
    }
}
