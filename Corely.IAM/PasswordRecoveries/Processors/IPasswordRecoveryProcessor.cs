using Corely.IAM.PasswordRecoveries.Models;

namespace Corely.IAM.PasswordRecoveries.Processors;

internal interface IPasswordRecoveryProcessor
{
    Task<RequestPasswordRecoveryResult> RequestPasswordRecoveryAsync(
        RequestPasswordRecoveryRequest request
    );

    Task<ValidatePasswordRecoveryTokenResult> ValidatePasswordRecoveryTokenAsync(
        ValidatePasswordRecoveryTokenRequest request
    );

    Task<ResetPasswordWithRecoveryResult> ResetPasswordWithRecoveryAsync(
        ResetPasswordWithRecoveryRequest request
    );
}
