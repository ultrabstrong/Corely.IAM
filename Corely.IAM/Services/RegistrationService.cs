using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Models.Extensions;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class RegistrationService(
    ILogger<RegistrationService> logger,
    IAccountProcessor accountProcessor,
    IUserProcessor userProcessor,
    IBasicAuthProcessor basicAuthProcessor,
    IGroupProcessor groupProcessor,
    IRoleProcessor roleProcessor,
    IPermissionProcessor permissionProcessor,
    IUserContextProvider userContextProvider,
    IUnitOfWorkProvider uowProvider
) : IRegistrationService
{
    private readonly ILogger<RegistrationService> _logger = logger.ThrowIfNull(nameof(logger));
    private readonly IAccountProcessor _accountProcessor = accountProcessor.ThrowIfNull(
        nameof(accountProcessor)
    );
    private readonly IUserProcessor _userProcessor = userProcessor.ThrowIfNull(
        nameof(userProcessor)
    );
    private readonly IBasicAuthProcessor _basicAuthProcessor = basicAuthProcessor.ThrowIfNull(
        nameof(basicAuthProcessor)
    );
    private readonly IGroupProcessor _groupProcessor = groupProcessor.ThrowIfNull(
        nameof(groupProcessor)
    );
    private readonly IRoleProcessor _roleProcessor = roleProcessor.ThrowIfNull(
        nameof(roleProcessor)
    );
    private readonly IPermissionProcessor _permissionProcessor = permissionProcessor.ThrowIfNull(
        nameof(permissionProcessor)
    );
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );
    private readonly IUnitOfWorkProvider _uowProvider = uowProvider.ThrowIfNull(
        nameof(uowProvider)
    );

    public async Task<RegisterUserResult> RegisterUserAsync(RegisterUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation("Registering user {User}", request.Username);

        var uowSucceeded = false;
        try
        {
            await _uowProvider.BeginAsync();

            var userResult = await _userProcessor.CreateUserAsync(
                new(request.Username, request.Email)
            );
            if (userResult.ResultCode != CreateUserResultCode.Success)
            {
                _logger.LogInformation(
                    "Registering user failed for username {Username}",
                    request.Username
                );
                return new RegisterUserResult(
                    RegisterUserResultCode.UserCreationError,
                    userResult.Message,
                    -1,
                    Guid.Empty
                );
            }

            var basicAuthResult = await _basicAuthProcessor.CreateBasicAuthAsync(
                new(userResult.CreatedId, request.Password)
            );
            if (basicAuthResult.ResultCode != CreateBasicAuthResultCode.Success)
            {
                _logger.LogInformation(
                    "Registering basic auth failed for username {Username}",
                    request.Username
                );
                return new RegisterUserResult(
                    RegisterUserResultCode.BasicAuthCreationError,
                    basicAuthResult.Message,
                    -1,
                    Guid.Empty
                );
            }

            await _uowProvider.CommitAsync();
            uowSucceeded = true;
            _logger.LogInformation(
                "User {Username} registered with Id {UserId}",
                request.Username,
                userResult.CreatedId
            );
            return new RegisterUserResult(
                RegisterUserResultCode.Success,
                string.Empty,
                userResult.CreatedId,
                userResult.CreatedPublicId
            );
        }
        finally
        {
            if (!uowSucceeded)
            {
                await _uowProvider.RollbackAsync();
            }
        }
    }

    public async Task<RegisterAccountResult> RegisterAccountAsync(RegisterAccountRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation("Registering account {AccountName}", request.AccountName);

        var uowSucceeded = false;
        try
        {
            await _uowProvider.BeginAsync();

            var ownerUserId = _userContextProvider.GetUserContext()!.UserId;
            var createAccountResult = await _accountProcessor.CreateAccountAsync(
                new(request.AccountName, ownerUserId)
            );
            if (createAccountResult.ResultCode != CreateAccountResultCode.Success)
            {
                _logger.LogInformation(
                    "Registering account failed for account name {AccountName}",
                    request.AccountName
                );
                return new RegisterAccountResult(
                    RegisterAccountResultCode.AccountCreationError,
                    createAccountResult.Message,
                    Guid.Empty
                );
            }

            var rolesResult = await _roleProcessor.CreateDefaultSystemRolesAsync(
                createAccountResult.CreatedId
            );
            await _permissionProcessor.CreateDefaultSystemPermissionsAsync(
                createAccountResult.CreatedId
            );

            var assignRoleResult = await _userProcessor.AssignRolesToUserAsync(
                new([rolesResult.OwnerRoleId], ownerUserId, BypassAuthorization: true)
            );
            if (assignRoleResult.ResultCode != AssignRolesToUserResultCode.Success)
            {
                _logger.LogInformation(
                    "Assigning owner role to user failed for account name {AccountName}",
                    request.AccountName
                );
                return new RegisterAccountResult(
                    RegisterAccountResultCode.SystemRoleAssignmentError,
                    assignRoleResult.Message,
                    Guid.Empty
                );
            }

            await _uowProvider.CommitAsync();
            uowSucceeded = true;

            _logger.LogInformation(
                "Account {AccountName} registered with Id {AccountId}",
                request.AccountName,
                createAccountResult.CreatedPublicId
            );
            return new RegisterAccountResult(
                RegisterAccountResultCode.Success,
                string.Empty,
                createAccountResult.CreatedPublicId
            );
        }
        finally
        {
            if (!uowSucceeded)
            {
                await _uowProvider.RollbackAsync();
            }
        }
    }

    public async Task<RegisterGroupResult> RegisterGroupAsync(RegisterGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation("Registering group {GroupName}", request.GroupName);

        var accountId = _userContextProvider.GetUserContext()!.AccountId!.Value;
        var result = await _groupProcessor.CreateGroupAsync(new(request.GroupName, accountId));
        if (result.ResultCode != CreateGroupResultCode.Success)
        {
            _logger.LogInformation(
                "Registering group failed for group name {GroupName}",
                request.GroupName
            );
            return new RegisterGroupResult(result.ResultCode, result.Message, -1);
        }

        _logger.LogInformation(
            "Group {GroupName} registered with Id {GroupId}",
            request.GroupName,
            result.CreatedId
        );

        return new RegisterGroupResult(result.ResultCode, string.Empty, result.CreatedId);
    }

    public async Task<RegisterRoleResult> RegisterRoleAsync(RegisterRoleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation("Registering role {RoleName}", request.RoleName);

        var accountId = _userContextProvider.GetUserContext()!.AccountId!.Value;
        var result = await _roleProcessor.CreateRoleAsync(new(request.RoleName, accountId));
        if (result.ResultCode != CreateRoleResultCode.Success)
        {
            _logger.LogInformation(
                "Registering role failed for role name {RoleName}",
                request.RoleName
            );
            return new RegisterRoleResult(result.ResultCode, result.Message, -1);
        }

        _logger.LogInformation(
            "Role {RoleName} registered with Id {RoleId}",
            request.RoleName,
            result.CreatedId
        );

        return new RegisterRoleResult(result.ResultCode, string.Empty, result.CreatedId);
    }

    public async Task<RegisterPermissionResult> RegisterPermissionAsync(
        RegisterPermissionRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation(
            "Registering permission for {ResourceType} - {ResourceId}",
            request.ResourceType,
            request.ResourceId
        );

        var accountId = _userContextProvider.GetUserContext()!.AccountId!.Value;
        var result = await _permissionProcessor.CreatePermissionAsync(
            new(
                accountId,
                request.ResourceType,
                request.ResourceId,
                request.Create,
                request.Read,
                request.Update,
                request.Delete,
                request.Execute,
                request.Description
            )
        );
        if (result.ResultCode != CreatePermissionResultCode.Success)
        {
            _logger.LogInformation(
                "Registering permission failed for {ResourceType} - {ResourceId}",
                request.ResourceType,
                request.ResourceId
            );
            return new RegisterPermissionResult(result.ResultCode, result.Message, -1);
        }

        _logger.LogInformation(
            "Permission for {ResourceType} - {ResourceId} registered with Id {PermissionId}",
            request.ResourceType,
            request.ResourceId,
            result.CreatedId
        );
        return new RegisterPermissionResult(result.ResultCode, string.Empty, result.CreatedId);
    }

    public async Task<RegisterUserWithAccountResult> RegisterUserWithAccountAsync(
        RegisterUserWithAccountRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var accountId = _userContextProvider.GetUserContext()!.AccountId!.Value;
        _logger.LogInformation(
            "Registering user {UserId} with account {AccountId}",
            request.UserId,
            accountId
        );

        var result = await _accountProcessor.AddUserToAccountAsync(new(request.UserId, accountId));

        if (result.ResultCode != AddUserToAccountResultCode.Success)
        {
            _logger.LogInformation(
                "Registering user {UserId} with account {AccountId} failed",
                request.UserId,
                accountId
            );
            return new RegisterUserWithAccountResult(
                result.ResultCode.ToRegisterUserWithAccountResultCode(),
                result.Message
            );
        }

        _logger.LogInformation(
            "User {UserId} registered with account {AccountId}",
            request.UserId,
            accountId
        );
        return new RegisterUserWithAccountResult(
            RegisterUserWithAccountResultCode.Success,
            string.Empty
        );
    }

    public async Task<RegisterUsersWithGroupResult> RegisterUsersWithGroupAsync(
        RegisterUsersWithGroupRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation(
            "Registering user ids {@UserIds} with group id {GroupId}",
            request.UserIds,
            request.GroupId
        );

        var result = await _groupProcessor.AddUsersToGroupAsync(
            new(request.UserIds, request.GroupId)
        );
        if (
            result.ResultCode != AddUsersToGroupResultCode.Success
            && result.ResultCode != AddUsersToGroupResultCode.PartialSuccess
        )
        {
            _logger.LogInformation(
                "Registering users with group failed for group id {GroupId}",
                request.GroupId
            );
            return new RegisterUsersWithGroupResult(
                result.ResultCode,
                result.Message ?? string.Empty,
                result.AddedUserCount,
                result.InvalidUserIds
            );
        }

        using (
            _logger.BeginScope(
                new Dictionary<string, object> { ["@InvalidUserIds"] = result.InvalidUserIds }
            )
        )
        {
            _logger.LogInformation(
                "Registered {RegisteredUserCount} users with group id {GroupId}",
                result.AddedUserCount,
                request.GroupId
            );
        }

        return new RegisterUsersWithGroupResult(
            result.ResultCode,
            result.Message ?? string.Empty,
            result.AddedUserCount,
            result.InvalidUserIds
        );
    }

    public async Task<RegisterRolesWithGroupResult> RegisterRolesWithGroupAsync(
        RegisterRolesWithGroupRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation(
            "Registering role ids {@RoleIds} with group id {GroupId}",
            request.RoleIds,
            request.GroupId
        );

        var result = await _groupProcessor.AssignRolesToGroupAsync(
            new(request.RoleIds, request.GroupId)
        );
        if (
            result.ResultCode != AssignRolesToGroupResultCode.Success
            && result.ResultCode != AssignRolesToGroupResultCode.PartialSuccess
        )
        {
            _logger.LogInformation(
                "Registering roles with group failed for group id {GroupId}",
                request.GroupId
            );
            return new RegisterRolesWithGroupResult(
                result.ResultCode,
                result.Message ?? string.Empty,
                result.AddedRoleCount,
                result.InvalidRoleIds
            );
        }

        using (
            _logger.BeginScope(
                new Dictionary<string, object> { ["@InvalidRoleIds"] = result.InvalidRoleIds }
            )
        )
        {
            _logger.LogInformation(
                "Registered {RegisteredRoleCount} roles with group id {GroupId}",
                result.AddedRoleCount,
                request.GroupId
            );
        }

        return new RegisterRolesWithGroupResult(
            result.ResultCode,
            result.Message ?? string.Empty,
            result.AddedRoleCount,
            result.InvalidRoleIds
        );
    }

    public async Task<RegisterRolesWithUserResult> RegisterRolesWithUserAsync(
        RegisterRolesWithUserRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation(
            "Registering role ids {@RoleIds} with user id {UserId}",
            request.RoleIds,
            request.UserId
        );

        var result = await _userProcessor.AssignRolesToUserAsync(
            new(request.RoleIds, request.UserId)
        );
        if (
            result.ResultCode != AssignRolesToUserResultCode.Success
            && result.ResultCode != AssignRolesToUserResultCode.PartialSuccess
        )
        {
            _logger.LogInformation(
                "Registering roles with user failed for user id {UserId}",
                request.UserId
            );
            return new RegisterRolesWithUserResult(
                result.ResultCode,
                result.Message ?? string.Empty,
                result.AddedRoleCount,
                result.InvalidRoleIds
            );
        }

        using (
            _logger.BeginScope(
                new Dictionary<string, object> { ["@InvalidRoleIds"] = result.InvalidRoleIds }
            )
        )
        {
            _logger.LogInformation(
                "Registered {RegisteredRoleCount} roles with user id {UserId}",
                result.AddedRoleCount,
                request.UserId
            );
        }

        return new RegisterRolesWithUserResult(
            result.ResultCode,
            result.Message ?? string.Empty,
            result.AddedRoleCount,
            result.InvalidRoleIds
        );
    }

    public async Task<RegisterPermissionsWithRoleResult> RegisterPermissionsWithRoleAsync(
        RegisterPermissionsWithRoleRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation(
            "Registering permission ids {@PermissionIds} with role id {RoleId}",
            request.PermissionIds,
            request.RoleId
        );

        var result = await _roleProcessor.AssignPermissionsToRoleAsync(
            new(request.PermissionIds, request.RoleId)
        );
        if (
            result.ResultCode != AssignPermissionsToRoleResultCode.Success
            && result.ResultCode != AssignPermissionsToRoleResultCode.PartialSuccess
        )
        {
            _logger.LogInformation(
                "Registering permissions with role failed for role id {RoleId}",
                request.RoleId
            );
            return new RegisterPermissionsWithRoleResult(
                result.ResultCode,
                result.Message ?? string.Empty,
                result.AddedPermissionCount,
                result.InvalidPermissionIds
            );
        }

        using (
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["@InvalidPermissionIds"] = result.InvalidPermissionIds,
                }
            )
        )
        {
            _logger.LogInformation(
                "Registered {RegisteredPermissionCount} permissions with role id {RoleId}",
                result.AddedPermissionCount,
                request.RoleId
            );
        }

        return new RegisterPermissionsWithRoleResult(
            result.ResultCode,
            result.Message ?? string.Empty,
            result.AddedPermissionCount,
            result.InvalidPermissionIds
        );
    }
}
