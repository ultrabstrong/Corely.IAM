using Corely.Common.Extensions;
using Corely.IAM.EmailVerifications.Models;
using Corely.IAM.Extensions;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.EmailVerifications.Processors;

internal class EmailVerificationProcessorTelemetryDecorator(
    IEmailVerificationProcessor inner,
    ILogger<EmailVerificationProcessorTelemetryDecorator> logger
) : IEmailVerificationProcessor
{
    private const string NAME = nameof(EmailVerificationProcessor);

    private readonly IEmailVerificationProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<EmailVerificationProcessorTelemetryDecorator> _logger =
        logger.ThrowIfNull(nameof(logger));

    public Task<RequestEmailVerificationResult> RequestForUserAsync(Guid userId) =>
        _logger.ExecuteWithLoggingAsync(NAME, userId, () => _inner.RequestForUserAsync(userId));

    public Task<RequestEmailVerificationResult> RequestForChallengeAsync(string challengeToken) =>
        _logger.ExecuteWithLoggingAsync(
            NAME,
            () => _inner.RequestForChallengeAsync(challengeToken)
        );

    public Task<VerifyEmailResult> VerifyEmailAsync(VerifyEmailRequest request) =>
        _logger.ExecuteWithLoggingAsync(
            NAME,
            () => _inner.VerifyEmailAsync(request),
            logResult: true
        );

    public Task<GetEmailVerificationStatusResult> GetStatusAsync(string challengeToken) =>
        _logger.ExecuteWithLoggingAsync(
            NAME,
            () => _inner.GetStatusAsync(challengeToken),
            logResult: true
        );

    public Task<string> CreateChallengeAsync(Guid userId, string deviceId, Guid? accountId) =>
        _logger.ExecuteWithLoggingAsync(
            NAME,
            userId,
            () => _inner.CreateChallengeAsync(userId, deviceId, accountId)
        );

    public Task<EmailVerificationChallengeOutcome> CompleteChallengeAsync(string challengeToken) =>
        _logger.ExecuteWithLoggingAsync(
            NAME,
            () => _inner.CompleteChallengeAsync(challengeToken),
            logResult: true
        );

    public Task MarkVerifiedAsync(Guid userId, EmailProof proof, Guid? keptIdentityId = null) =>
        _logger.ExecuteWithLoggingAsync(
            NAME,
            userId,
            () => _inner.MarkVerifiedAsync(userId, proof, keptIdentityId)
        );

    public Task<bool> MustConfirmEmailAsync(Guid userId) =>
        _logger.ExecuteWithLoggingAsync(
            NAME,
            userId,
            () => _inner.MustConfirmEmailAsync(userId),
            logResult: true
        );

    public Task<bool> ReplaceUnconfirmedHolderAsync(string email) =>
        _logger.ExecuteWithLoggingAsync(
            NAME,
            () => _inner.ReplaceUnconfirmedHolderAsync(email),
            logResult: true
        );
}
