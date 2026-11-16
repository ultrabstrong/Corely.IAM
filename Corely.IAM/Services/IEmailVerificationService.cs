using Corely.IAM.EmailVerifications.Models;

namespace Corely.IAM.Services;

public interface IEmailVerificationService
{
    Task<RequestEmailVerificationResult> RequestEmailVerificationAsync();

    Task<RequestEmailVerificationResult> RequestEmailVerificationForUserAsync(Guid userId);

    Task<RequestEmailVerificationResult> ResendEmailVerificationAsync(string challengeToken);

    Task<GetEmailVerificationStatusResult> GetEmailVerificationStatusAsync(string challengeToken);

    Task<VerifyEmailResult> VerifyEmailAsync(VerifyEmailRequest request);

    Task<RejectEmailVerificationResult> RejectEmailVerificationAsync(VerifyEmailRequest request);
}
