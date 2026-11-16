using Corely.IAM.EmailVerifications.Models;

namespace Corely.IAM.EmailVerifications.Processors;

internal interface IEmailVerificationProcessor
{
    Task<RequestEmailVerificationResult> RequestForUserAsync(Guid userId);

    Task<RequestEmailVerificationResult> RequestForChallengeAsync(string challengeToken);

    Task<VerifyEmailResult> VerifyEmailAsync(VerifyEmailRequest request);

    Task<GetEmailVerificationStatusResult> GetStatusAsync(string challengeToken);

    Task<string> CreateChallengeAsync(Guid userId, string deviceId, Guid? accountId);

    Task<EmailVerificationChallengeOutcome> CompleteChallengeAsync(string challengeToken);

    Task MarkVerifiedAsync(Guid userId, EmailProof proof, Guid? keptIdentityId = null);

    Task<bool> ReplaceUnconfirmedHolderAsync(string email);

    Task<bool> MustConfirmEmailAsync(Guid userId);
}
