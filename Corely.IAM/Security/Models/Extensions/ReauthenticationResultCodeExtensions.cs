using Corely.IAM.Models;

namespace Corely.IAM.Security.Models.Extensions;

internal static class ReauthenticationResultCodeExtensions
{
    extension(ReauthenticationResultCode resultCode)
    {
        public RegisterPasswordResultCode ToRegisterPasswordResultCode() =>
            resultCode switch
            {
                ReauthenticationResultCode.Success => RegisterPasswordResultCode.Success,
                ReauthenticationResultCode.TotpRequiredError =>
                    RegisterPasswordResultCode.TotpRequiredError,
                ReauthenticationResultCode.InvalidTotpCodeError =>
                    RegisterPasswordResultCode.InvalidTotpCodeError,
                _ => throw Unmapped(resultCode),
            };

        public ModifyPasswordResultCode ToModifyPasswordResultCode() =>
            resultCode switch
            {
                ReauthenticationResultCode.Success => ModifyPasswordResultCode.Success,
                ReauthenticationResultCode.BasicAuthNotFoundError =>
                    ModifyPasswordResultCode.BasicAuthNotFoundError,
                ReauthenticationResultCode.InvalidCurrentPasswordError =>
                    ModifyPasswordResultCode.InvalidCurrentPasswordError,
                ReauthenticationResultCode.UserLockedError =>
                    ModifyPasswordResultCode.UserLockedError,
                ReauthenticationResultCode.TotpRequiredError =>
                    ModifyPasswordResultCode.TotpRequiredError,
                ReauthenticationResultCode.InvalidTotpCodeError =>
                    ModifyPasswordResultCode.InvalidTotpCodeError,
                _ => throw Unmapped(resultCode),
            };

        public DeregisterBasicAuthResultCode ToDeregisterBasicAuthResultCode() =>
            resultCode switch
            {
                ReauthenticationResultCode.Success => DeregisterBasicAuthResultCode.Success,
                ReauthenticationResultCode.BasicAuthNotFoundError =>
                    DeregisterBasicAuthResultCode.NotFoundError,
                ReauthenticationResultCode.InvalidCurrentPasswordError =>
                    DeregisterBasicAuthResultCode.InvalidCurrentPasswordError,
                ReauthenticationResultCode.UserLockedError =>
                    DeregisterBasicAuthResultCode.UserLockedError,
                ReauthenticationResultCode.TotpRequiredError =>
                    DeregisterBasicAuthResultCode.TotpRequiredError,
                ReauthenticationResultCode.InvalidTotpCodeError =>
                    DeregisterBasicAuthResultCode.InvalidTotpCodeError,
                _ => throw Unmapped(resultCode),
            };

        public CompromiseAccountKeyResultCode ToCompromiseAccountKeyResultCode() =>
            resultCode switch
            {
                ReauthenticationResultCode.Success => CompromiseAccountKeyResultCode.Success,
                ReauthenticationResultCode.BasicAuthNotFoundError =>
                    CompromiseAccountKeyResultCode.BasicAuthNotFoundError,
                ReauthenticationResultCode.InvalidCurrentPasswordError =>
                    CompromiseAccountKeyResultCode.InvalidCurrentPasswordError,
                ReauthenticationResultCode.UserLockedError =>
                    CompromiseAccountKeyResultCode.UserLockedError,
                ReauthenticationResultCode.TotpRequiredError =>
                    CompromiseAccountKeyResultCode.TotpRequiredError,
                ReauthenticationResultCode.InvalidTotpCodeError =>
                    CompromiseAccountKeyResultCode.InvalidTotpCodeError,
                ReauthenticationResultCode.FederatedSignInRequiredError =>
                    CompromiseAccountKeyResultCode.FederatedSignInRequiredError,
                ReauthenticationResultCode.NoReauthenticationMethodError =>
                    CompromiseAccountKeyResultCode.NoReauthenticationMethodError,
                _ => throw Unmapped(resultCode),
            };

        public RevealAccountKeyResultCode ToRevealAccountKeyResultCode() =>
            resultCode switch
            {
                ReauthenticationResultCode.Success => RevealAccountKeyResultCode.Success,
                ReauthenticationResultCode.BasicAuthNotFoundError =>
                    RevealAccountKeyResultCode.BasicAuthNotFoundError,
                ReauthenticationResultCode.InvalidCurrentPasswordError =>
                    RevealAccountKeyResultCode.InvalidCurrentPasswordError,
                ReauthenticationResultCode.UserLockedError =>
                    RevealAccountKeyResultCode.UserLockedError,
                ReauthenticationResultCode.TotpRequiredError =>
                    RevealAccountKeyResultCode.TotpRequiredError,
                ReauthenticationResultCode.InvalidTotpCodeError =>
                    RevealAccountKeyResultCode.InvalidTotpCodeError,
                ReauthenticationResultCode.FederatedSignInRequiredError =>
                    RevealAccountKeyResultCode.FederatedSignInRequiredError,
                ReauthenticationResultCode.NoReauthenticationMethodError =>
                    RevealAccountKeyResultCode.NoReauthenticationMethodError,
                _ => throw Unmapped(resultCode),
            };
    }

    private static ArgumentOutOfRangeException Unmapped(ReauthenticationResultCode resultCode) =>
        new(
            nameof(resultCode),
            resultCode,
            $"Unmapped {nameof(ReauthenticationResultCode)}: {resultCode}"
        );
}
