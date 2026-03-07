using Corely.IAM.Invitations.Models;
using Corely.IAM.Models;

namespace Corely.IAM.Services;

public interface IRegistrationService
{
    Task<RegisterUserResult> RegisterUserAsync(RegisterUserRequest request);
    Task<RegisterAccountResult> RegisterAccountAsync(RegisterAccountRequest request);
    Task<RegisterGroupResult> RegisterGroupAsync(RegisterGroupRequest request);
    Task<RegisterRoleResult> RegisterRoleAsync(RegisterRoleRequest request);
    Task<RegisterPermissionResult> RegisterPermissionAsync(RegisterPermissionRequest request);
    Task<RegisterUserWithAccountResult> RegisterUserWithAccountAsync(
        RegisterUserWithAccountRequest request
    );
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
    Task<CreateInvitationResult> CreateInvitationAsync(CreateInvitationRequest request);
    Task<AcceptInvitationResult> AcceptInvitationAsync(AcceptInvitationRequest request);
    Task<RevokeInvitationResult> RevokeInvitationAsync(Guid invitationId);
    Task<RetrieveListResult<Invitation>> ListInvitationsAsync(ListInvitationsRequest request);
}
