namespace Corely.IAM.Security.Enums;

internal enum SessionTokenRenewalCode
{
    Ready,
    InvalidToken,
    SessionExpired,
    AlreadyRotated,
    Reused,
}
