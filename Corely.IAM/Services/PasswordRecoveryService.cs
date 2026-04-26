using Corely.Common.Extensions;
using Corely.IAM.PasswordRecoveries.Models;
using Corely.IAM.PasswordRecoveries.Processors;

namespace Corely.IAM.Services;

internal class PasswordRecoveryService(IPasswordRecoveryProcessor passwordRecoveryProcessor)
    : IPasswordRecoveryService
{
    private readonly IPasswordRecoveryProcessor _passwordRecoveryProcessor =
        passwordRecoveryProcessor.ThrowIfNull(nameof(passwordRecoveryProcessor));

    public Task<RequestPasswordRecoveryResult> RequestPasswordRecoveryAsync(
        RequestPasswordRecoveryRequest request
    ) => _passwordRecoveryProcessor.RequestPasswordRecoveryAsync(request);

    public Task<ValidatePasswordRecoveryTokenResult> ValidatePasswordRecoveryTokenAsync(
        ValidatePasswordRecoveryTokenRequest request
    ) => _passwordRecoveryProcessor.ValidatePasswordRecoveryTokenAsync(request);

    public Task<ResetPasswordWithRecoveryResult> ResetPasswordWithRecoveryAsync(
        ResetPasswordWithRecoveryRequest request
    ) => _passwordRecoveryProcessor.ResetPasswordWithRecoveryAsync(request);
}
