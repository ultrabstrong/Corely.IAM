using Corely.IAM.Models;

namespace Corely.IAM.Services;

public interface IRegistrationService
{
    Task<RegisterUserResult> RegisterUserAsync(RegisterUserRequest request);
    Task<RegisterAccountResult> RegisterAccountAsync(RegisterAccountRequest request);
    Task<RegisterGroupResult> RegisterGroupAsync(RegisterGroupRequest request);
    Task<RegisterRoleResult> RegisterRoleAsync(RegisterRoleRequest request);
    Task<RegisterPermissionResult> RegisterPermissionAsync(RegisterPermissionRequest request);
    Task<RegisterUsersWithGroupResult> RegisterUsersWithGroupAsync(
        RegisterUsersWithGroupRequest request
    );
    Task<RegisterRolesWithGroupResult> RegisterRolesWithGroupAsync(
        RegisterRolesWithGroupRequest request
    );
    Task<RegisterRolesWithUserResult> RegisterRolesWithUserAsync(
        RegisterRolesWithUserRequest request
    );
    Task<RegisterPermissionsWithRoleResult> RegisterPermissionsWithRoleAsync(
        RegisterPermissionsWithRoleRequest request
    );
}
