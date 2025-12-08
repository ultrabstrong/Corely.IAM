using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Groups.Mappers;
using Corely.IAM.Groups.Models;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Users.Entities;
using Corely.IAM.Validators;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Groups.Processors;

internal class GroupProcessor : IGroupProcessor
{
    private readonly IRepo<GroupEntity> _groupRepo;
    private readonly IReadonlyRepo<AccountEntity> _accountRepo;
    private readonly IReadonlyRepo<UserEntity> _userRepo;
    private readonly IReadonlyRepo<RoleEntity> _roleRepo;
    private readonly IValidationProvider _validationProvider;
    private readonly ILogger<GroupProcessor> _logger;

    public GroupProcessor(
        IRepo<GroupEntity> groupRepo,
        IReadonlyRepo<AccountEntity> accountRepo,
        IReadonlyRepo<UserEntity> userRepo,
        IReadonlyRepo<RoleEntity> roleRepo,
        IValidationProvider validationProvider,
        ILogger<GroupProcessor> logger
    )
    {
        _groupRepo = groupRepo.ThrowIfNull(nameof(groupRepo));
        _accountRepo = accountRepo.ThrowIfNull(nameof(accountRepo));
        _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
        _roleRepo = roleRepo.ThrowIfNull(nameof(roleRepo));
        _validationProvider = validationProvider.ThrowIfNull(nameof(validationProvider));
        _logger = logger.ThrowIfNull(nameof(logger));
    }

    public async Task<CreateGroupResult> CreateGroupAsync(CreateGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var group = request.ToGroup();
        _validationProvider.ThrowIfInvalid(group);

        if (await _groupRepo.AnyAsync(g => g.AccountId == group.AccountId && g.Name == group.Name))
        {
            _logger.LogWarning("Group with name {GroupName} already exists", group.Name);
            return new CreateGroupResult(
                CreateGroupResultCode.GroupExistsError,
                $"Group with name {group.Name} already exists",
                -1
            );
        }

        var accountEntity = await _accountRepo.GetAsync(a => a.Id == group.AccountId);
        if (accountEntity == null)
        {
            _logger.LogWarning("Account with Id {AccountId} not found", group.AccountId);
            return new CreateGroupResult(
                CreateGroupResultCode.AccountNotFoundError,
                $"Account with Id {group.AccountId} not found",
                -1
            );
        }

        var groupEntity = group.ToEntity();
        var created = await _groupRepo.CreateAsync(groupEntity);

        return new CreateGroupResult(CreateGroupResultCode.Success, string.Empty, created.Id);
    }

    public async Task<AddUsersToGroupResult> AddUsersToGroupAsync(AddUsersToGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var groupEntity = await _groupRepo.GetAsync(g => g.Id == request.GroupId);
        if (groupEntity == null)
        {
            _logger.LogWarning("Group with Id {GroupId} not found", request.GroupId);
            return new AddUsersToGroupResult(
                AddUsersToGroupResultCode.GroupNotFoundError,
                $"Group with Id {request.GroupId} not found",
                0,
                request.UserIds
            );
        }

        var userEntities = await _userRepo.ListAsync(u =>
            request.UserIds.Contains(u.Id)
            && !u.Groups!.Any(g => g.Id == groupEntity.Id)
            && u.Accounts!.Any(a => a.Id == groupEntity.AccountId)
        );

        if (userEntities.Count == 0)
        {
            _logger.LogInformation(
                "All user ids are invalid (not found, from different account, or already exist in group) : {@InvalidUserIds}",
                request.UserIds
            );
            return new AddUsersToGroupResult(
                AddUsersToGroupResultCode.InvalidUserIdsError,
                "All user ids are invalid (not found, from different account, or already exist in group)",
                0,
                request.UserIds
            );
        }

        groupEntity.Users ??= [];
        foreach (var user in userEntities)
        {
            groupEntity.Users.Add(user);
        }

        await _groupRepo.UpdateAsync(groupEntity);

        var invalidUserIds = request.UserIds.Except(userEntities.Select(u => u.Id)).ToList();
        if (invalidUserIds.Count > 0)
        {
            _logger.LogInformation(
                "Some user ids are invalid (not found, from different account, or already exist in group) : {@InvalidUserIds}",
                invalidUserIds
            );
            return new AddUsersToGroupResult(
                AddUsersToGroupResultCode.PartialSuccess,
                "Some user ids are invalid (not found, from different account, or already exist in group)",
                userEntities.Count,
                invalidUserIds
            );
        }

        return new AddUsersToGroupResult(
            AddUsersToGroupResultCode.Success,
            string.Empty,
            userEntities.Count,
            invalidUserIds
        );
    }

    public async Task<AssignRolesToGroupResult> AssignRolesToGroupAsync(
        AssignRolesToGroupRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var groupEntity = await _groupRepo.GetAsync(g => g.Id == request.GroupId);
        if (groupEntity == null)
        {
            _logger.LogWarning("Group with Id {GroupId} not found", request.GroupId);
            return new AssignRolesToGroupResult(
                AssignRolesToGroupResultCode.GroupNotFoundError,
                $"Group with Id {request.GroupId} not found",
                0,
                request.RoleIds
            );
        }

        var roleEntities = await _roleRepo.ListAsync(r =>
            request.RoleIds.Contains(r.Id)
            && !r.Groups!.Any(g => g.Id == groupEntity.Id)
            && r.Account!.Id == groupEntity.AccountId
        );

        if (roleEntities.Count == 0)
        {
            _logger.LogInformation(
                "All role ids are invalid (not found, from different account, or already assigned to group) : {@InvalidRoleIds}",
                request.RoleIds
            );
            return new AssignRolesToGroupResult(
                AssignRolesToGroupResultCode.InvalidRoleIdsError,
                "All role ids are invalid (not found, from different account, or already assigned to group)",
                0,
                request.RoleIds
            );
        }

        groupEntity.Roles ??= [];
        foreach (var role in roleEntities)
        {
            groupEntity.Roles.Add(role);
        }

        await _groupRepo.UpdateAsync(groupEntity);

        var invalidRoleIds = request.RoleIds.Except(roleEntities.Select(r => r.Id)).ToList();
        if (invalidRoleIds.Count > 0)
        {
            _logger.LogInformation(
                "Some role ids are invalid (not found, from different account, or already assigned to group) : {@InvalidRoleIds}",
                invalidRoleIds
            );
            return new AssignRolesToGroupResult(
                AssignRolesToGroupResultCode.PartialSuccess,
                "Some role ids are invalid (not found, from different account, or already assigned to group)",
                roleEntities.Count,
                invalidRoleIds
            );
        }

        return new AssignRolesToGroupResult(
            AssignRolesToGroupResultCode.Success,
            string.Empty,
            roleEntities.Count,
            invalidRoleIds
        );
    }

    public async Task<DeleteGroupResult> DeleteGroupAsync(int groupId)
    {
        var groupEntity = await _groupRepo.GetAsync(g => g.Id == groupId);
        if (groupEntity == null)
        {
            _logger.LogWarning("Group with Id {GroupId} not found", groupId);
            return new DeleteGroupResult(
                DeleteGroupResultCode.GroupNotFoundError,
                $"Group with Id {groupId} not found"
            );
        }

        await _groupRepo.DeleteAsync(groupEntity);

        _logger.LogInformation("Group with Id {GroupId} deleted", groupId);
        return new DeleteGroupResult(DeleteGroupResultCode.Success, string.Empty);
    }
}
