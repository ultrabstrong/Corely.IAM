using Corely.IAM.Models;

namespace Corely.IAM.Services;

public interface IDeregistrationService
{
    Task<DeregisterPermissionResult> DeregisterPermissionAsync(DeregisterPermissionRequest request);
    Task<DeregisterRoleResult> DeregisterRoleAsync(DeregisterRoleRequest request);
    Task<DeregisterUserResult> DegisterUserAsync(DeregisterUserRequest request);
    Task<DeregisterAccountResult> DegisterAccountAsync(DeregisterAccountRequest request);
}
