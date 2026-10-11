using Corely.IAM.Models;

namespace Corely.IAM.BasicAuths.Models.Extensions;

internal static class UpdateBasicAuthResultCodeExtensions
{
    extension(UpdateBasicAuthResultCode resultCode)
    {
        public ModifyPasswordResultCode ToModifyPasswordResultCode() =>
            resultCode switch
            {
                UpdateBasicAuthResultCode.Success => ModifyPasswordResultCode.Success,
                UpdateBasicAuthResultCode.BasicAuthNotFoundError =>
                    ModifyPasswordResultCode.BasicAuthNotFoundError,
                UpdateBasicAuthResultCode.UnauthorizedError =>
                    ModifyPasswordResultCode.UnauthorizedError,
                UpdateBasicAuthResultCode.PasswordValidationError =>
                    ModifyPasswordResultCode.PasswordValidationError,
                UpdateBasicAuthResultCode.ValidationError =>
                    ModifyPasswordResultCode.ValidationError,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(resultCode),
                    resultCode,
                    $"Unmapped {nameof(UpdateBasicAuthResultCode)}: {resultCode}"
                ),
            };
    }
}
