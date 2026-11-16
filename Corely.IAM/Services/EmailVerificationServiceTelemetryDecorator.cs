using Corely.Common.Extensions;
using Corely.IAM.EmailVerifications.Models;
using Corely.IAM.Extensions;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class EmailVerificationServiceTelemetryDecorator(
    IEmailVerificationService inner,
    ILogger<EmailVerificationServiceTelemetryDecorator> logger
) : IEmailVerificationService
{
    private readonly IEmailVerificationService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<EmailVerificationServiceTelemetryDecorator> _logger =
        logger.ThrowIfNull(nameof(logger));

    public async Task<RequestEmailVerificationResult> RequestEmailVerificationAsync() =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(EmailVerificationService),
            () => _inner.RequestEmailVerificationAsync(),
            logResult: false
        );

    public async Task<RequestEmailVerificationResult> RequestEmailVerificationForUserAsync(
        Guid userId
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(EmailVerificationService),
            userId,
            () => _inner.RequestEmailVerificationForUserAsync(userId),
            logResult: false
        );

    public async Task<RequestEmailVerificationResult> ResendEmailVerificationAsync(
        string challengeToken
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(EmailVerificationService),
            () => _inner.ResendEmailVerificationAsync(challengeToken),
            logResult: false
        );

    public async Task<GetEmailVerificationStatusResult> GetEmailVerificationStatusAsync(
        string challengeToken
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(EmailVerificationService),
            () => _inner.GetEmailVerificationStatusAsync(challengeToken),
            logResult: true
        );

    public async Task<VerifyEmailResult> VerifyEmailAsync(VerifyEmailRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(EmailVerificationService),
            () => _inner.VerifyEmailAsync(request),
            logResult: true
        );
}
