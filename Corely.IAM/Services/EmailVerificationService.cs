using Corely.Common.Extensions;
using Corely.IAM.EmailVerifications.Models;
using Corely.IAM.EmailVerifications.Processors;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.Services;

internal class EmailVerificationService(
    IEmailVerificationProcessor emailVerificationProcessor,
    IUserContextProvider userContextProvider
) : IEmailVerificationService
{
    private readonly IEmailVerificationProcessor _emailVerificationProcessor =
        emailVerificationProcessor.ThrowIfNull(nameof(emailVerificationProcessor));
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );

    public Task<RequestEmailVerificationResult> RequestEmailVerificationAsync() =>
        _emailVerificationProcessor.RequestForUserAsync(
            _userContextProvider.GetUserContext()!.User!.Id
        );

    public Task<RequestEmailVerificationResult> RequestEmailVerificationForUserAsync(Guid userId) =>
        _emailVerificationProcessor.RequestForUserAsync(userId);

    public Task<RequestEmailVerificationResult> ResendEmailVerificationAsync(
        string challengeToken
    ) => _emailVerificationProcessor.RequestForChallengeAsync(challengeToken);

    public Task<GetEmailVerificationStatusResult> GetEmailVerificationStatusAsync(
        string challengeToken
    ) => _emailVerificationProcessor.GetStatusAsync(challengeToken);

    public Task<RejectEmailVerificationResult> RejectEmailVerificationAsync(
        VerifyEmailRequest request
    ) => _emailVerificationProcessor.RejectAsync(request);

    public Task<VerifyEmailResult> VerifyEmailAsync(VerifyEmailRequest request) =>
        _emailVerificationProcessor.VerifyEmailAsync(request);
}
