namespace Corely.IAM.TotpAuths.Models;

internal record VerifyTotpOrRecoveryCodeRequest(Guid UserId, string Code);
