using Corely.IAM.Filtering;
using Corely.IAM.Filtering.Ordering;
using Corely.IAM.Models;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Users.Processors;

internal interface IUserProcessor
{
    Task<CreateUserResult> CreateUserAsync(CreateUserRequest request);
    Task<GetUserResult> GetUserAsync(Guid userId);
    Task<UpdateUserResult> UpdateUserAsync(User user);
    Task<GetAsymmetricKeyResult> GetAsymmetricSignatureVerificationKeyAsync(Guid userId);
    Task<AssignRolesToUserResult> AssignRolesToUserAsync(AssignRolesToUserRequest request);
    Task<RemoveRolesFromUserResult> RemoveRolesFromUserAsync(RemoveRolesFromUserRequest request);
    Task<DeleteUserResult> DeleteUserAsync(Guid userId);
    Task<ListResult<User>> ListUsersAsync(
        FilterBuilder<User>? filter,
        OrderBuilder<User>? order,
        int skip,
        int take
    );
    Task<GetResult<User>> GetUserByIdAsync(Guid userId, bool hydrate);
}
