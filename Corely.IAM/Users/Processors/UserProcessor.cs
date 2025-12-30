using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Mappers;
using Corely.IAM.Users.Models;
using Corely.IAM.Validators;
using Corely.Security.Encryption.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Users.Processors;

internal class UserProcessor(
    IRepo<UserEntity> userRepo,
    IReadonlyRepo<RoleEntity> roleRepo,
    IUserOwnershipProcessor userOwnershipProcessor,
    ISecurityProvider securityProcessor,
    ISymmetricEncryptionProviderFactory encryptionProviderFactory,
    IValidationProvider validationProvider,
    ILogger<UserProcessor> logger
) : IUserProcessor
{
    private readonly IRepo<UserEntity> _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
    private readonly IReadonlyRepo<RoleEntity> _roleRepo = roleRepo.ThrowIfNull(nameof(roleRepo));
    private readonly IUserOwnershipProcessor _userOwnershipProcessor =
        userOwnershipProcessor.ThrowIfNull(nameof(userOwnershipProcessor));
    private readonly ISecurityProvider _securityProcessor = securityProcessor.ThrowIfNull(
        nameof(securityProcessor)
    );
    private readonly ISymmetricEncryptionProviderFactory _encryptionProviderFactory =
        encryptionProviderFactory.ThrowIfNull(nameof(encryptionProviderFactory));
    private readonly IValidationProvider _validationProvider = validationProvider.ThrowIfNull(
        nameof(validationProvider)
    );
    private readonly ILogger<UserProcessor> _logger = logger.ThrowIfNull(nameof(logger));

    public async Task<CreateUserResult> CreateUserAsync(CreateUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var user = request.ToUser();
        _validationProvider.ThrowIfInvalid(user);

        var existingUser = await _userRepo.GetAsync(u =>
            u.Username == request.Username || u.Email == request.Email
        );

        if (existingUser != null)
        {
            bool usernameExists = existingUser.Username == request.Username;
            bool emailExists = existingUser.Email == request.Email;

            if (usernameExists)
                _logger.LogInformation(
                    "User already exists with Username {ExistingUsername}",
                    existingUser.Username
                );
            if (emailExists)
                _logger.LogInformation(
                    "User already exists with Email {ExistingEmail}",
                    existingUser.Email
                );

            string usernameExistsMessage = usernameExists
                ? $"Username {request.Username} already exists."
                : string.Empty;
            string emailExistsMessage = emailExists
                ? $"Email {request.Email} already exists."
                : string.Empty;

            return new CreateUserResult(
                CreateUserResultCode.UserExistsError,
                $"{usernameExistsMessage} {emailExistsMessage}".Trim(),
                Guid.Empty
            );
        }

        user.SymmetricKeys = [_securityProcessor.GetSymmetricEncryptionKeyEncryptedWithSystemKey()];
        user.AsymmetricKeys =
        [
            _securityProcessor.GetAsymmetricEncryptionKeyEncryptedWithSystemKey(),
            _securityProcessor.GetAsymmetricSignatureKeyEncryptedWithSystemKey(),
        ];

        var userEntity = user.ToEntity(_encryptionProviderFactory);
        var created = await _userRepo.CreateAsync(userEntity);

        return new CreateUserResult(CreateUserResultCode.Success, string.Empty, created.Id);
    }

    public async Task<GetUserResult> GetUserAsync(Guid userId)
    {
        var userEntity = await _userRepo.GetAsync(u => u.Id == userId);

        if (userEntity == null)
        {
            _logger.LogInformation("User with Id {UserId} not found", userId);
            return new GetUserResult(
                GetUserResultCode.UserNotFoundError,
                $"User with Id {userId} not found",
                null
            );
        }

        return new GetUserResult(GetUserResultCode.Success, string.Empty, userEntity.ToModel());
    }

    public async Task<UpdateUserResult> UpdateUserAsync(User user)
    {
        _validationProvider.ThrowIfInvalid(user);
        var userEntity = user.ToEntity();
        await _userRepo.UpdateAsync(userEntity);
        return new UpdateUserResult(UpdateUserResultCode.Success, string.Empty);
    }

    public async Task<GetAsymmetricKeyResult> GetAsymmetricSignatureVerificationKeyAsync(
        Guid userId
    )
    {
        var userEntity = await _userRepo.GetAsync(
            u => u.Id == userId,
            include: q => q.Include(u => u.AsymmetricKeys)
        );

        if (userEntity == null)
        {
            _logger.LogWarning("User with Id {UserId} not found", userId);
            return new GetAsymmetricKeyResult(
                GetAsymmetricKeyResultCode.UserNotFoundError,
                $"User with Id {userId} not found",
                null
            );
        }

        var signatureKey = userEntity.AsymmetricKeys?.FirstOrDefault(k =>
            k.KeyUsedFor == Security.Enums.KeyUsedFor.Signature
        );
        if (signatureKey == null)
        {
            _logger.LogWarning(
                "User with Id {UserId} does not have an asymmetric signature key",
                userId
            );
            return new GetAsymmetricKeyResult(
                GetAsymmetricKeyResultCode.KeyNotFoundError,
                $"User with Id {userId} does not have an asymmetric signature key",
                null
            );
        }

        return new GetAsymmetricKeyResult(
            GetAsymmetricKeyResultCode.Success,
            string.Empty,
            signatureKey.PublicKey
        );
    }

    public async Task<AssignRolesToUserResult> AssignRolesToUserAsync(
        AssignRolesToUserRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var userEntity = await _userRepo.GetAsync(
            u => u.Id == request.UserId,
            include: q => q.Include(u => u.Accounts)
        );
        if (userEntity == null)
        {
            _logger.LogWarning("User with Id {UserId} not found", request.UserId);
            return new AssignRolesToUserResult(
                AssignRolesToUserResultCode.UserNotFoundError,
                $"User with Id {request.UserId} not found",
                0,
                request.RoleIds
            );
        }
        var roleEntities = await _roleRepo.ListAsync(r =>
            request.RoleIds.Contains(r.Id) && !r.Users!.Any(u => u.Id == userEntity.Id)
        );

        roleEntities =
        [
            .. roleEntities.Where(r => userEntity.Accounts?.Any(a => a.Id == r.AccountId) ?? false),
        ];

        if (roleEntities.Count == 0)
        {
            _logger.LogInformation(
                "All role ids are invalid (not found, already assigned to user, or from different account) : {@InvalidRoleIds}",
                request.RoleIds
            );
            return new AssignRolesToUserResult(
                AssignRolesToUserResultCode.InvalidRoleIdsError,
                "All role ids are invalid (not found, already assigned to user, or from different account)",
                0,
                request.RoleIds
            );
        }

        userEntity.Roles ??= [];
        foreach (var role in roleEntities)
        {
            userEntity.Roles.Add(role);
        }

        await _userRepo.UpdateAsync(userEntity);

        var invalidRoleIds = request.RoleIds.Except(roleEntities.Select(r => r.Id)).ToList();
        if (invalidRoleIds.Count > 0)
        {
            _logger.LogInformation(
                "Some role ids are invalid (not found, already assigned to user, or from different account) : {@InvalidRoleIds}",
                invalidRoleIds
            );
            return new AssignRolesToUserResult(
                AssignRolesToUserResultCode.PartialSuccess,
                "Some role ids are invalid (not found, already assigned to user, or from different account)",
                roleEntities.Count,
                invalidRoleIds
            );
        }

        return new AssignRolesToUserResult(
            AssignRolesToUserResultCode.Success,
            string.Empty,
            roleEntities.Count,
            invalidRoleIds
        );
    }

    public async Task<RemoveRolesFromUserResult> RemoveRolesFromUserAsync(
        RemoveRolesFromUserRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var userEntity = await _userRepo.GetAsync(
            u => u.Id == request.UserId,
            include: q => q.Include(u => u.Roles).Include(u => u.Accounts)
        );

        if (userEntity == null)
        {
            _logger.LogWarning("User with Id {UserId} not found", request.UserId);
            return new RemoveRolesFromUserResult(
                RemoveRolesFromUserResultCode.UserNotFoundError,
                $"User with Id {request.UserId} not found",
                0,
                request.RoleIds
            );
        }

        var rolesToRemove =
            userEntity.Roles?.Where(r => request.RoleIds.Contains(r.Id)).ToList() ?? [];

        if (rolesToRemove.Count == 0)
        {
            _logger.LogInformation(
                "All role ids are invalid (not found or not assigned to user) : {@InvalidRoleIds}",
                request.RoleIds
            );
            return new RemoveRolesFromUserResult(
                RemoveRolesFromUserResultCode.InvalidRoleIdsError,
                "All role ids are invalid (not found or not assigned to user)",
                0,
                request.RoleIds
            );
        }

        // Role removal rules:
        // 1. If all roles are not the owner role -> proceed with removal
        // 2. If any role is owner role and user IS NOT sole owner -> remove the role
        // 3. If any role is owner role and user IS sole owner and user has multiple ownership sources -> remove the role
        // 4. If any role is owner role and user IS sole owner and user has single ownership source -> block
        var blockedOwnerRoleIds = new List<Guid>();
        var ownerRoles = rolesToRemove
            .Where(r => r.Name == RoleConstants.OWNER_ROLE_NAME && r.IsSystemDefined)
            .ToList();

        if (ownerRoles.Count > 0)
        {
            foreach (var ownerRole in ownerRoles)
            {
                var soleOwnerResult = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(
                    request.UserId,
                    ownerRole.AccountId
                );

                // Block if: user is sole owner AND has only single ownership source (the direct role being removed)
                if (soleOwnerResult.IsSoleOwner && soleOwnerResult.HasSingleOwnershipSource)
                {
                    blockedOwnerRoleIds.Add(ownerRole.Id);
                    _logger.LogWarning(
                        "Cannot remove owner role {RoleId} from user {UserId} - user is sole owner of account {AccountId} with no ownership elsewhere",
                        ownerRole.Id,
                        request.UserId,
                        ownerRole.AccountId
                    );
                }
            }

            // If ALL roles being removed are blocked owner roles, return error
            if (blockedOwnerRoleIds.Count == rolesToRemove.Count)
            {
                return new RemoveRolesFromUserResult(
                    RemoveRolesFromUserResultCode.UserIsSoleOwnerError,
                    "Cannot remove owner role(s) from user - user is the sole owner of the account(s) with no ownership elsewhere.",
                    0,
                    [],
                    blockedOwnerRoleIds
                );
            }

            // Filter out blocked roles
            rolesToRemove = [.. rolesToRemove.Where(r => !blockedOwnerRoleIds.Contains(r.Id))];
        }

        foreach (var role in rolesToRemove)
        {
            userEntity.Roles!.Remove(role);
        }

        if (rolesToRemove.Count > 0)
        {
            await _userRepo.UpdateAsync(userEntity);
        }

        // Calculate invalid IDs (requested but not actually removed, excluding blocked)
        var invalidRoleIds = request
            .RoleIds.Except(rolesToRemove.Select(r => r.Id))
            .Except(blockedOwnerRoleIds)
            .ToList();

        // Return appropriate result
        if (blockedOwnerRoleIds.Count > 0 || invalidRoleIds.Count > 0)
        {
            _logger.LogInformation(
                "Some roles were not removed: invalid {@InvalidRoleIds}, blocked owner {@BlockedOwnerRoleIds}",
                invalidRoleIds,
                blockedOwnerRoleIds
            );
            return new RemoveRolesFromUserResult(
                RemoveRolesFromUserResultCode.PartialSuccess,
                blockedOwnerRoleIds.Count > 0
                    ? "Some roles could not be removed (invalid or user is sole owner)"
                    : "Some role ids are invalid (not found or not assigned to user)",
                rolesToRemove.Count,
                invalidRoleIds,
                blockedOwnerRoleIds
            );
        }

        return new RemoveRolesFromUserResult(
            RemoveRolesFromUserResultCode.Success,
            string.Empty,
            rolesToRemove.Count,
            []
        );
    }

    public async Task<DeleteUserResult> DeleteUserAsync(Guid userId)
    {
        var userEntity = await _userRepo.GetAsync(
            u => u.Id == userId,
            include: q => q.Include(u => u.Accounts)
        );

        if (userEntity == null)
        {
            _logger.LogWarning("User with Id {UserId} not found", userId);
            return new DeleteUserResult(
                DeleteUserResultCode.UserNotFoundError,
                $"User with Id {userId} not found"
            );
        }

        if (userEntity.Accounts != null)
        {
            foreach (var account in userEntity.Accounts)
            {
                var soleOwnerResult = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(
                    userId,
                    account.Id
                );
                if (soleOwnerResult.IsSoleOwner)
                {
                    _logger.LogInformation(
                        "User with Id {UserId} is the sole owner of account {AccountId} and cannot be deleted",
                        userId,
                        account.Id
                    );
                    return new DeleteUserResult(
                        DeleteUserResultCode.UserIsSoleAccountOwnerError,
                        $"User is the sole owner of account '{account.AccountName}' (Id: {account.Id}) and cannot be deleted"
                    );
                }
            }
        }

        await _userRepo.DeleteAsync(userEntity);

        _logger.LogInformation("User with Id {UserId} deleted", userId);
        return new DeleteUserResult(DeleteUserResultCode.Success, string.Empty);
    }
}
