using Corely.Common.Extensions;
using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;
using Corely.IAM.Groups.Models;
using Corely.IAM.Invitations.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Services;

internal class RegistrationServiceAuthorizationDecorator(
    IRegistrationService inner,
    IAuthorizationProvider authorizationProvider
) : IRegistrationService
{
    private readonly IRegistrationService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public Task<RegisterUserResult> RegisterUserAsync(RegisterUserRequest request) =>
        _inner.RegisterUserAsync(request);

    public async Task<RegisterAccountResult> RegisterAccountAsync(RegisterAccountRequest request) =>
        _authorizationProvider.HasUserContext()
            ? await _inner.RegisterAccountAsync(request)
            : new RegisterAccountResult(
                RegisterAccountResultCode.UnauthorizedError,
                "Unauthorized to create account",
                Guid.Empty
            );

    public async Task<RegisterGroupResult> RegisterGroupAsync(RegisterGroupRequest request) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.RegisterGroupAsync(request)
            : new RegisterGroupResult(
                CreateGroupResultCode.UnauthorizedError,
                "Unauthorized to create group",
                Guid.Empty
            );

    public async Task<RegisterRoleResult> RegisterRoleAsync(RegisterRoleRequest request) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.RegisterRoleAsync(request)
            : new RegisterRoleResult(
                CreateRoleResultCode.UnauthorizedError,
                "Unauthorized to create role",
                Guid.Empty
            );

    public async Task<RegisterPermissionResult> RegisterPermissionAsync(
        RegisterPermissionRequest request
    ) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.RegisterPermissionAsync(request)
            : new RegisterPermissionResult(
                CreatePermissionResultCode.UnauthorizedError,
                "Unauthorized to create permission",
                Guid.Empty
            );

    public async Task<RegisterUserWithAccountResult> RegisterUserWithAccountAsync(
        RegisterUserWithAccountRequest request
    ) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.RegisterUserWithAccountAsync(request)
            : new RegisterUserWithAccountResult(
                RegisterUserWithAccountResultCode.UnauthorizedError,
                "Unauthorized to add user to account"
            );

    public async Task<RegisterUsersWithGroupResult> RegisterUsersWithGroupAsync(
        RegisterUsersWithGroupRequest request
    ) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.RegisterUsersWithGroupAsync(request)
            : new RegisterUsersWithGroupResult(
                AddUsersToGroupResultCode.UnauthorizedError,
                "Unauthorized to add users to group",
                0,
                []
            );

    public async Task<RegisterRolesWithGroupResult> RegisterRolesWithGroupAsync(
        RegisterRolesWithGroupRequest request
    ) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.RegisterRolesWithGroupAsync(request)
            : new RegisterRolesWithGroupResult(
                AssignRolesToGroupResultCode.UnauthorizedError,
                "Unauthorized to assign roles to group",
                0,
                []
            );

    public async Task<RegisterRolesWithUserResult> RegisterRolesWithUserAsync(
        RegisterRolesWithUserRequest request
    ) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.RegisterRolesWithUserAsync(request)
            : new RegisterRolesWithUserResult(
                AssignRolesToUserResultCode.UnauthorizedError,
                "Unauthorized to assign roles to user",
                0,
                []
            );

    public async Task<RegisterPermissionsWithRoleResult> RegisterPermissionsWithRoleAsync(
        RegisterPermissionsWithRoleRequest request
    ) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.RegisterPermissionsWithRoleAsync(request)
            : new RegisterPermissionsWithRoleResult(
                AssignPermissionsToRoleResultCode.UnauthorizedError,
                "Unauthorized to assign permissions to role",
                0,
                []
            );

    public async Task<CreateInvitationResult> CreateInvitationAsync(
        CreateInvitationRequest request
    ) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.CreateInvitationAsync(request)
            : new CreateInvitationResult(
                CreateInvitationResultCode.UnauthorizedError,
                "Unauthorized to create invitation",
                null,
                null
            );

    public async Task<AcceptInvitationResult> AcceptInvitationAsync(
        AcceptInvitationRequest request
    ) =>
        _authorizationProvider.HasUserContext()
            ? await _inner.AcceptInvitationAsync(request)
            : new AcceptInvitationResult(
                AcceptInvitationResultCode.UnauthorizedError,
                "Unauthorized to accept invitation",
                null
            );

    public async Task<RevokeInvitationResult> RevokeInvitationAsync(Guid invitationId) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.RevokeInvitationAsync(invitationId)
            : new RevokeInvitationResult(
                RevokeInvitationResultCode.UnauthorizedError,
                "Unauthorized to revoke invitation"
            );

    public async Task<RetrieveListResult<Invitation>> ListInvitationsAsync(
        ListInvitationsRequest request
    ) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.ListInvitationsAsync(request)
            : new RetrieveListResult<Invitation>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to list invitations",
                null
            );
}
