using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Services;

public interface IModificationService
{
    Task<ModifyResult> ModifyAccountAsync(UpdateAccountRequest request);
    Task<ModifyResult> ModifyUserAsync(UpdateUserRequest request);
    Task<ModifyResult> ModifyGroupAsync(UpdateGroupRequest request);
    Task<ModifyResult> ModifyRoleAsync(UpdateRoleRequest request);
    Task<ModifyResult> ModifyPermissionAsync(UpdatePermissionRequest request);
    Task<ModifyResult> RotateAccountKeyAsync(RotateAccountKeyRequest request);
    Task<ModifyResult> RotateCurrentUserKeyAsync(KeyType keyType);
}
