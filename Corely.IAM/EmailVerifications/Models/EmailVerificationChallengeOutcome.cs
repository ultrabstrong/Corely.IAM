namespace Corely.IAM.EmailVerifications.Models;

internal enum EmailVerificationChallengeOutcomeCode
{
    Confirmed,
    Pending,
    NotFound,
}

internal sealed record EmailVerificationChallengeOutcome(
    EmailVerificationChallengeOutcomeCode ResultCode,
    Guid UserId = default,
    string DeviceId = "",
    Guid? AccountId = null
);
