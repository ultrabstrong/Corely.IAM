using Corely.IAM.Users.Models;

namespace Corely.IAM.Users.Processors;

internal interface IUserProcessor
{
    Task<CreateUserResult> CreateUserAsync(CreateUserRequest request);
    Task<GetUserResult> GetUserAsync(int userId);
    Task<UpdateUserResult> UpdateUserAsync(User user);
    Task<GetAsymmetricKeyResult> GetAsymmetricSignatureVerificationKeyAsync(int userId);
    Task<AssignRolesToUserResult> AssignRolesToUserAsync(AssignRolesToUserRequest request);
    Task<RemoveRolesFromUserResult> RemoveRolesFromUserAsync(RemoveRolesFromUserRequest request);
    Task<DeleteUserResult> DeleteUserAsync(int userId);
}
