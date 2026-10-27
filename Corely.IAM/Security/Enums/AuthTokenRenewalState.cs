namespace Corely.IAM.Security.Enums;

internal enum AuthTokenRenewalState
{
    Renewable,
    RecentlyReplaced,
    Reused,
    Revoked,
    Expired,
}
