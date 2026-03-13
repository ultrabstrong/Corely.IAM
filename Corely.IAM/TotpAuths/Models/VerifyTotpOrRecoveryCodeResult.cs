namespace Corely.IAM.TotpAuths.Models;

internal enum VerifyTotpOrRecoveryCodeResultCode
{
    TotpCodeValid,
    RecoveryCodeValid,
    InvalidCodeError,
    NotFoundError,
}

internal record VerifyTotpOrRecoveryCodeResult(
    VerifyTotpOrRecoveryCodeResultCode ResultCode,
    string Message
);
