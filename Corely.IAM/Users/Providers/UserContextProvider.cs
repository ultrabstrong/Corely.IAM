using Corely.Common.Extensions;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Users.Providers;

internal class UserContextProvider(IAuthenticationProvider authenticationProvider)
    : IUserContextProvider,
        IUserContextSetter
{
    private readonly IAuthenticationProvider _authenticationProvider =
        authenticationProvider.ThrowIfNull(nameof(authenticationProvider));
    private UserContext? _userContext;

    public UserContext? GetUserContext() => _userContext;

    public async Task<UserAuthTokenValidationResultCode> SetUserContextAsync(string authToken)
    {
        ArgumentNullException.ThrowIfNull(authToken, nameof(authToken));

        var validationResult = await _authenticationProvider.ValidateUserAuthTokenAsync(authToken);

        if (
            validationResult.ResultCode != UserAuthTokenValidationResultCode.Success
            || validationResult.User == null
        )
        {
            return validationResult.ResultCode;
        }

        if (string.IsNullOrWhiteSpace(validationResult.DeviceId))
        {
            return UserAuthTokenValidationResultCode.MissingDeviceIdClaim;
        }

        _userContext = new UserContext(
            validationResult.User,
            validationResult.CurrentAccount,
            validationResult.DeviceId,
            validationResult.AvailableAccounts
        );
        return UserAuthTokenValidationResultCode.Success;
    }

    public void SetUserContext(UserContext context) => _userContext = context;

    public void ClearUserContext(Guid userId) =>
        _userContext = _userContext?.User?.Id == userId ? null : _userContext;
}
