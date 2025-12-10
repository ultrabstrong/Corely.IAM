using Corely.IAM.Models;

namespace Corely.IAM.Services;

public interface IDeregistrationService
{
    Task<DeregisterPermissionResult> DeregisterPermissionAsync(DeregisterPermissionRequest request);
    Task<DeregisterRoleResult> DeregisterRoleAsync(DeregisterRoleRequest request);
    Task<DeregisterGroupResult> DeregisterGroupAsync(DeregisterGroupRequest request);
    Task<DeregisterUsersFromGroupResult> DeregisterUsersFromGroupAsync(
        DeregisterUsersFromGroupRequest request
    );
    Task<DeregisterUserResult> DeregisterUserAsync(DeregisterUserRequest request);
    Task<DeregisterUserFromAccountResult> DeregisterUserFromAccountAsync(
        DeregisterUserFromAccountRequest request
    );
    Task<DeregisterAccountResult> DeregisterAccountAsync(DeregisterAccountRequest request);
}
