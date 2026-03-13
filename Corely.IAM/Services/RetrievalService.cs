using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.GoogleAuths.Processors;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.TotpAuths.Processors;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Corely.Security.Encryption.Factories;

namespace Corely.IAM.Services;

internal class RetrievalService(
    IPermissionProcessor permissionProcessor,
    IGroupProcessor groupProcessor,
    IRoleProcessor roleProcessor,
    IUserProcessor userProcessor,
    IAccountProcessor accountProcessor,
    ISecurityProvider securityProvider,
    ITotpAuthProcessor totpAuthProcessor,
    IGoogleAuthProcessor googleAuthProcessor,
    ISymmetricEncryptionProviderFactory symmetricEncryptionProviderFactory,
    IUserContextProvider userContextProvider
) : IRetrievalService
{
    private readonly IPermissionProcessor _permissionProcessor = permissionProcessor.ThrowIfNull(
        nameof(permissionProcessor)
    );
    private readonly IGroupProcessor _groupProcessor = groupProcessor.ThrowIfNull(
        nameof(groupProcessor)
    );
    private readonly IRoleProcessor _roleProcessor = roleProcessor.ThrowIfNull(
        nameof(roleProcessor)
    );
    private readonly IUserProcessor _userProcessor = userProcessor.ThrowIfNull(
        nameof(userProcessor)
    );
    private readonly IAccountProcessor _accountProcessor = accountProcessor.ThrowIfNull(
        nameof(accountProcessor)
    );
    private readonly ISecurityProvider _securityProvider = securityProvider.ThrowIfNull(
        nameof(securityProvider)
    );
    private readonly ITotpAuthProcessor _totpAuthProcessor = totpAuthProcessor.ThrowIfNull(
        nameof(totpAuthProcessor)
    );
    private readonly IGoogleAuthProcessor _googleAuthProcessor = googleAuthProcessor.ThrowIfNull(
        nameof(googleAuthProcessor)
    );
    private readonly ISymmetricEncryptionProviderFactory _symmetricEncryptionProviderFactory =
        symmetricEncryptionProviderFactory.ThrowIfNull(nameof(symmetricEncryptionProviderFactory));
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );

    public Task<RetrieveListResult<Permission>> ListPermissionsAsync(
        ListPermissionsRequest request
    ) => WrapListResultAsync(_permissionProcessor.ListPermissionsAsync(request));

    public async Task<RetrieveSingleResult<Permission>> GetPermissionAsync(
        Guid permissionId,
        bool hydrate = false
    )
    {
        var result = await _permissionProcessor.GetPermissionByIdAsync(permissionId, hydrate);
        var effectivePermissions = await GetEffectivePermissionsAsync(
            PermissionConstants.PERMISSION_RESOURCE_TYPE,
            permissionId
        );
        return new RetrieveSingleResult<Permission>(
            result.ResultCode,
            result.Message,
            result.Data,
            effectivePermissions
        );
    }

    public Task<RetrieveListResult<Group>> ListGroupsAsync(ListGroupsRequest request) =>
        WrapListResultAsync(_groupProcessor.ListGroupsAsync(request));

    public async Task<RetrieveSingleResult<Group>> GetGroupAsync(Guid groupId, bool hydrate = false)
    {
        var result = await _groupProcessor.GetGroupByIdAsync(groupId, hydrate);
        var effectivePermissions = await GetEffectivePermissionsAsync(
            PermissionConstants.GROUP_RESOURCE_TYPE,
            groupId
        );
        return new RetrieveSingleResult<Group>(
            result.ResultCode,
            result.Message,
            result.Data,
            effectivePermissions
        );
    }

    public Task<RetrieveListResult<Role>> ListRolesAsync(ListRolesRequest request) =>
        WrapListResultAsync(_roleProcessor.ListRolesAsync(request));

    public async Task<RetrieveSingleResult<Role>> GetRoleAsync(Guid roleId, bool hydrate = false)
    {
        var result = await _roleProcessor.GetRoleByIdAsync(roleId, hydrate);
        var effectivePermissions = await GetEffectivePermissionsAsync(
            PermissionConstants.ROLE_RESOURCE_TYPE,
            roleId
        );
        return new RetrieveSingleResult<Role>(
            result.ResultCode,
            result.Message,
            result.Data,
            effectivePermissions
        );
    }

    public Task<RetrieveListResult<User>> ListUsersAsync(ListUsersRequest request) =>
        WrapListResultAsync(_userProcessor.ListUsersAsync(request));

    public async Task<RetrieveSingleResult<User>> GetUserAsync(Guid userId, bool hydrate = false)
    {
        var result = await _userProcessor.GetUserByIdAsync(userId, hydrate);
        var effectivePermissions = await GetEffectivePermissionsAsync(
            PermissionConstants.USER_RESOURCE_TYPE,
            userId
        );
        return new RetrieveSingleResult<User>(
            result.ResultCode,
            result.Message,
            result.Data,
            effectivePermissions
        );
    }

    public Task<RetrieveListResult<Account>> ListAccountsAsync(ListAccountsRequest request) =>
        WrapListResultAsync(_accountProcessor.ListAccountsAsync(request));

    public async Task<RetrieveSingleResult<Account>> GetAccountAsync(
        Guid accountId,
        bool hydrate = false
    )
    {
        var result = await _accountProcessor.GetAccountByIdAsync(accountId, hydrate);
        var effectivePermissions = await GetEffectivePermissionsAsync(
            PermissionConstants.ACCOUNT_RESOURCE_TYPE,
            accountId
        );
        return new RetrieveSingleResult<Account>(
            result.ResultCode,
            result.Message,
            result.Data,
            effectivePermissions
        );
    }

    public async Task<TotpStatusResult> GetTotpStatusAsync()
    {
        var context = _userContextProvider.GetUserContext();
        return await _totpAuthProcessor.GetTotpStatusAsync(context!.User.Id);
    }

    public async Task<AuthMethodsResult> GetAuthMethodsAsync()
    {
        var context = _userContextProvider.GetUserContext();
        return await _googleAuthProcessor.GetAuthMethodsAsync(context!.User.Id);
    }

    private async Task<List<EffectivePermission>> GetEffectivePermissionsAsync(
        string resourceType,
        Guid resourceId
    )
    {
        var userContext = _userContextProvider.GetUserContext();
        if (userContext?.CurrentAccount == null)
            return [];

        return await _permissionProcessor.GetEffectivePermissionsForUserAsync(
            resourceType,
            resourceId,
            userContext.User.Id,
            userContext.CurrentAccount.Id
        );
    }

    private static async Task<RetrieveListResult<T>> WrapListResultAsync<T>(
        Task<ListResult<T>> resultTask
    )
    {
        var result = await resultTask;
        return new RetrieveListResult<T>(result.ResultCode, result.Message, result.Data);
    }

    public async Task<
        RetrieveSingleResult<IIamSymmetricEncryptionProvider>
    > GetAccountSymmetricEncryptionProviderAsync(Guid accountId)
    {
        var keysResult = await _accountProcessor.GetAccountKeysAsync(accountId);
        if (keysResult.ResultCode != RetrieveResultCode.Success || keysResult.Data == null)
        {
            return new RetrieveSingleResult<IIamSymmetricEncryptionProvider>(
                keysResult.ResultCode,
                keysResult.Message,
                null,
                null
            );
        }

        var symmetricKeyEntity = keysResult.Data.SymmetricKeys?.FirstOrDefault(k =>
            k.KeyUsedFor == KeyUsedFor.Encryption
        );
        if (symmetricKeyEntity == null)
        {
            return new RetrieveSingleResult<IIamSymmetricEncryptionProvider>(
                RetrieveResultCode.NotFoundError,
                "Symmetric encryption key not found for account",
                null,
                null
            );
        }

        var symmetricKey = symmetricKeyEntity.ToModel(_symmetricEncryptionProviderFactory);
        var provider = _securityProvider.BuildSymmetricEncryptionProvider(symmetricKey);
        return new RetrieveSingleResult<IIamSymmetricEncryptionProvider>(
            RetrieveResultCode.Success,
            string.Empty,
            provider,
            null
        );
    }

    public async Task<
        RetrieveSingleResult<IIamAsymmetricEncryptionProvider>
    > GetAccountAsymmetricEncryptionProviderAsync(Guid accountId)
    {
        var keysResult = await _accountProcessor.GetAccountKeysAsync(accountId);
        if (keysResult.ResultCode != RetrieveResultCode.Success || keysResult.Data == null)
        {
            return new RetrieveSingleResult<IIamAsymmetricEncryptionProvider>(
                keysResult.ResultCode,
                keysResult.Message,
                null,
                null
            );
        }

        var asymmetricKeyEntity = keysResult.Data.AsymmetricKeys?.FirstOrDefault(k =>
            k.KeyUsedFor == KeyUsedFor.Encryption
        );
        if (asymmetricKeyEntity == null)
        {
            return new RetrieveSingleResult<IIamAsymmetricEncryptionProvider>(
                RetrieveResultCode.NotFoundError,
                "Asymmetric encryption key not found for account",
                null,
                null
            );
        }

        var asymmetricKey = asymmetricKeyEntity.ToModel(_symmetricEncryptionProviderFactory);
        var provider = _securityProvider.BuildAsymmetricEncryptionProvider(asymmetricKey);
        return new RetrieveSingleResult<IIamAsymmetricEncryptionProvider>(
            RetrieveResultCode.Success,
            string.Empty,
            provider,
            null
        );
    }

    public async Task<
        RetrieveSingleResult<IIamAsymmetricSignatureProvider>
    > GetAccountAsymmetricSignatureProviderAsync(Guid accountId)
    {
        var keysResult = await _accountProcessor.GetAccountKeysAsync(accountId);
        if (keysResult.ResultCode != RetrieveResultCode.Success || keysResult.Data == null)
        {
            return new RetrieveSingleResult<IIamAsymmetricSignatureProvider>(
                keysResult.ResultCode,
                keysResult.Message,
                null,
                null
            );
        }

        var asymmetricKeyEntity = keysResult.Data.AsymmetricKeys?.FirstOrDefault(k =>
            k.KeyUsedFor == KeyUsedFor.Signature
        );
        if (asymmetricKeyEntity == null)
        {
            return new RetrieveSingleResult<IIamAsymmetricSignatureProvider>(
                RetrieveResultCode.NotFoundError,
                "Asymmetric signature key not found for account",
                null,
                null
            );
        }

        var asymmetricKey = asymmetricKeyEntity.ToModel(_symmetricEncryptionProviderFactory);
        var provider = _securityProvider.BuildAsymmetricSignatureProvider(asymmetricKey);
        return new RetrieveSingleResult<IIamAsymmetricSignatureProvider>(
            RetrieveResultCode.Success,
            string.Empty,
            provider,
            null
        );
    }

    public async Task<
        RetrieveSingleResult<IIamSymmetricEncryptionProvider>
    > GetUserSymmetricEncryptionProviderAsync()
    {
        var keysResult = await _userProcessor.GetCurrentUserKeysAsync();
        if (keysResult.ResultCode != RetrieveResultCode.Success || keysResult.Data == null)
        {
            return new RetrieveSingleResult<IIamSymmetricEncryptionProvider>(
                keysResult.ResultCode,
                keysResult.Message,
                null,
                null
            );
        }

        var symmetricKeyEntity = keysResult.Data.SymmetricKeys?.FirstOrDefault(k =>
            k.KeyUsedFor == KeyUsedFor.Encryption
        );
        if (symmetricKeyEntity == null)
        {
            return new RetrieveSingleResult<IIamSymmetricEncryptionProvider>(
                RetrieveResultCode.NotFoundError,
                "Symmetric encryption key not found for user",
                null,
                null
            );
        }

        var symmetricKey = symmetricKeyEntity.ToModel(_symmetricEncryptionProviderFactory);
        var provider = _securityProvider.BuildSymmetricEncryptionProvider(symmetricKey);
        return new RetrieveSingleResult<IIamSymmetricEncryptionProvider>(
            RetrieveResultCode.Success,
            string.Empty,
            provider,
            null
        );
    }

    public async Task<
        RetrieveSingleResult<IIamAsymmetricEncryptionProvider>
    > GetUserAsymmetricEncryptionProviderAsync()
    {
        var keysResult = await _userProcessor.GetCurrentUserKeysAsync();
        if (keysResult.ResultCode != RetrieveResultCode.Success || keysResult.Data == null)
        {
            return new RetrieveSingleResult<IIamAsymmetricEncryptionProvider>(
                keysResult.ResultCode,
                keysResult.Message,
                null,
                null
            );
        }

        var asymmetricKeyEntity = keysResult.Data.AsymmetricKeys?.FirstOrDefault(k =>
            k.KeyUsedFor == KeyUsedFor.Encryption
        );
        if (asymmetricKeyEntity == null)
        {
            return new RetrieveSingleResult<IIamAsymmetricEncryptionProvider>(
                RetrieveResultCode.NotFoundError,
                "Asymmetric encryption key not found for user",
                null,
                null
            );
        }

        var asymmetricKey = asymmetricKeyEntity.ToModel(_symmetricEncryptionProviderFactory);
        var provider = _securityProvider.BuildAsymmetricEncryptionProvider(asymmetricKey);
        return new RetrieveSingleResult<IIamAsymmetricEncryptionProvider>(
            RetrieveResultCode.Success,
            string.Empty,
            provider,
            null
        );
    }

    public async Task<
        RetrieveSingleResult<IIamAsymmetricSignatureProvider>
    > GetUserAsymmetricSignatureProviderAsync()
    {
        var keysResult = await _userProcessor.GetCurrentUserKeysAsync();
        if (keysResult.ResultCode != RetrieveResultCode.Success || keysResult.Data == null)
        {
            return new RetrieveSingleResult<IIamAsymmetricSignatureProvider>(
                keysResult.ResultCode,
                keysResult.Message,
                null,
                null
            );
        }

        var asymmetricKeyEntity = keysResult.Data.AsymmetricKeys?.FirstOrDefault(k =>
            k.KeyUsedFor == KeyUsedFor.Signature
        );
        if (asymmetricKeyEntity == null)
        {
            return new RetrieveSingleResult<IIamAsymmetricSignatureProvider>(
                RetrieveResultCode.NotFoundError,
                "Asymmetric signature key not found for user",
                null,
                null
            );
        }

        var asymmetricKey = asymmetricKeyEntity.ToModel(_symmetricEncryptionProviderFactory);
        var provider = _securityProvider.BuildAsymmetricSignatureProvider(asymmetricKey);
        return new RetrieveSingleResult<IIamAsymmetricSignatureProvider>(
            RetrieveResultCode.Success,
            string.Empty,
            provider,
            null
        );
    }
}
