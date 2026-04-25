using Corely.IAM.Users.Models;

namespace Corely.IAM.Users.Providers;

internal class UserContextProvider : IUserContextProvider, IUserContextSetter
{
    private UserContext? _userContext;

    public UserContext? GetUserContext() => _userContext;

    public void SetUserContext(UserContext context) => _userContext = context;

    public void SetSystemContext(string deviceId) =>
        _userContext = new UserContext(isSystemContext: true, deviceId);

    public void ClearUserContext(Guid userId) =>
        _userContext = _userContext?.User?.Id == userId ? null : _userContext;
}
