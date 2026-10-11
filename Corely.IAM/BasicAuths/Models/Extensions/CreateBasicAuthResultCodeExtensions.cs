using Corely.IAM.Models;

namespace Corely.IAM.BasicAuths.Models.Extensions;

internal static class CreateBasicAuthResultCodeExtensions
{
    extension(CreateBasicAuthResultCode resultCode)
    {
        public RegisterPasswordResultCode ToRegisterPasswordResultCode() =>
            resultCode switch
            {
                CreateBasicAuthResultCode.Success => RegisterPasswordResultCode.Success,
                CreateBasicAuthResultCode.BasicAuthExistsError =>
                    RegisterPasswordResultCode.BasicAuthExistsError,
                CreateBasicAuthResultCode.PasswordValidationError =>
                    RegisterPasswordResultCode.PasswordValidationError,
                CreateBasicAuthResultCode.ValidationError =>
                    RegisterPasswordResultCode.ValidationError,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(resultCode),
                    resultCode,
                    $"Unmapped {nameof(CreateBasicAuthResultCode)}: {resultCode}"
                ),
            };
    }
}
