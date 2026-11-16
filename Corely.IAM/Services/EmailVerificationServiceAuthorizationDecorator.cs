using Corely.Common.Extensions;
using Corely.IAM.EmailVerifications.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.Services;

internal class EmailVerificationServiceAuthorizationDecorator(
    IEmailVerificationService inner,
    IAuthorizationProvider authorizationProvider,
    IUserContextProvider userContextProvider
) : IEmailVerificationService
{
    private readonly IEmailVerificationService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );

    public async Task<RequestEmailVerificationResult> RequestEmailVerificationAsync() =>
        _authorizationProvider.IsNonSystemUserContext()
            ? await _inner.RequestEmailVerificationAsync()
            : Unauthorized();

    public async Task<RequestEmailVerificationResult> RequestEmailVerificationForUserAsync(
        Guid userId
    ) =>
        _userContextProvider.GetUserContext()?.IsSystemContext == true
            ? await _inner.RequestEmailVerificationForUserAsync(userId)
            : Unauthorized();

    public Task<RequestEmailVerificationResult> ResendEmailVerificationAsync(
        string challengeToken
    ) => _inner.ResendEmailVerificationAsync(challengeToken);

    public Task<GetEmailVerificationStatusResult> GetEmailVerificationStatusAsync(
        string challengeToken
    ) => _inner.GetEmailVerificationStatusAsync(challengeToken);

    public Task<RejectEmailVerificationResult> RejectEmailVerificationAsync(
        VerifyEmailRequest request
    ) => _inner.RejectEmailVerificationAsync(request);

    public Task<VerifyEmailResult> VerifyEmailAsync(VerifyEmailRequest request) =>
        _inner.VerifyEmailAsync(request);

    private static RequestEmailVerificationResult Unauthorized() =>
        new(RequestEmailVerificationResultCode.UnauthorizedError, "Unauthorized");
}
