using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Services;

public interface IModificationService
{
    Task<ModifyResult> ModifyAccountAsync(UpdateAccountRequest request);
    Task<ModifyResult> ModifyUserAsync(UpdateUserRequest request);
    Task<ModifyResult> ModifyGroupAsync(UpdateGroupRequest request);
    Task<ModifyResult> ModifyRoleAsync(UpdateRoleRequest request);
}
