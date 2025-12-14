using Corely.IAM.Models;

namespace Corely.IAM.Services;

public interface IDeregistrationService
{
    Task<DeregisterUserResult> DeregisterUserAsync();
    Task<DeregisterAccountResult> DeregisterAccountAsync();
    Task<DeregisterGroupResult> DeregisterGroupAsync(DeregisterGroupRequest request);
    Task<DeregisterRoleResult> DeregisterRoleAsync(DeregisterRoleRequest request);
    Task<DeregisterPermissionResult> DeregisterPermissionAsync(DeregisterPermissionRequest request);
    Task<DeregisterUserFromAccountResult> DeregisterUserFromAccountAsync(
        DeregisterUserFromAccountRequest request
    );
    Task<DeregisterUsersFromGroupResult> DeregisterUsersFromGroupAsync(
        DeregisterUsersFromGroupRequest request
    );
    Task<DeregisterRolesFromGroupResult> DeregisterRolesFromGroupAsync(
        DeregisterRolesFromGroupRequest request
    );
    Task<DeregisterRolesFromUserResult> DeregisterRolesFromUserAsync(
        DeregisterRolesFromUserRequest request
    );
    Task<DeregisterPermissionsFromRoleResult> DeregisterPermissionsFromRoleAsync(
        DeregisterPermissionsFromRoleRequest request
    );
}
