using Corely.IAM.Models;

namespace Corely.IAM.Accounts.Models.Extensions;

internal static class AddUserToAccountResultCodeExtensions
{
    extension(AddUserToAccountResultCode resultCode)
    {
        public RegisterUserWithAccountResultCode ToRegisterUserWithAccountResultCode() =>
            resultCode switch
            {
                AddUserToAccountResultCode.Success => RegisterUserWithAccountResultCode.Success,
                AddUserToAccountResultCode.UserNotFoundError =>
                    RegisterUserWithAccountResultCode.UserNotFoundError,
                AddUserToAccountResultCode.AccountNotFoundError =>
                    RegisterUserWithAccountResultCode.AccountNotFoundError,
                AddUserToAccountResultCode.UserAlreadyInAccountError =>
                    RegisterUserWithAccountResultCode.UserAlreadyInAccountError,
                AddUserToAccountResultCode.UnauthorizedError =>
                    RegisterUserWithAccountResultCode.UnauthorizedError,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(resultCode),
                    resultCode,
                    $"Unmapped {nameof(AddUserToAccountResultCode)}: {resultCode}"
                ),
            };
    }
}
