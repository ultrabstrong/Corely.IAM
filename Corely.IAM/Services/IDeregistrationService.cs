using Corely.IAM.Models;

namespace Corely.IAM.Services;

public interface IDeregistrationService
{
    Task<DeregisterUserResult> DeregisterUserAsync(DeregisterUserRequest request);
    Task<DeregisterAccountResult> DeregisterAccountAsync(DeregisterAccountRequest request);
    Task<DeregisterGroupResult> DeregisterGroupAsync(DeregisterGroupRequest request);
    Task<DeregisterRoleResult> DeregisterRoleAsync(DeregisterRoleRequest request);
    Task<DeregisterPermissionResult> DeregisterPermissionAsync(DeregisterPermissionRequest request);
    Task<DeregisterUserFromAccountResult> DeregisterUserFromAccountAsync(
        DeregisterUserFromAccountRequest request
    );
    Task<DeregisterUsersFromGroupResult> DeregisterUsersFromGroupAsync(
        DeregisterUsersFromGroupRequest request
    );
}
