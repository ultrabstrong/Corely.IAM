using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;
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
    Task<RetrieveListResult<Invitation>> ListInvitationsAsync(
        Guid accountId,
        FilterBuilder<Invitation>? filter = null,
        OrderBuilder<Invitation>? order = null,
        int skip = 0,
        int take = 25,
        InvitationStatus? statusFilter = null
    );
}
