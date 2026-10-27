using Corely.IAM.ApiKeys.Enums;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Authentication;

public class ApiKeyTests : IAsyncLifetime
{
    private const string DEVICE = "api-client";

    private readonly IamScenario _scenario = new(
        new Dictionary<string, string?>
        {
            ["SecurityOptions:ApiKeysEnabled"] = "true",
            ["SecurityOptions:ApiKeyResourceTypes:0"] = User.RESOURCE_TYPE,
            ["SecurityOptions:ApiKeyResourceTypes:1"] = ApiKey.RESOURCE_TYPE,
        }
    );

    public ValueTask InitializeAsync() => _scenario.InitializeAsync();

    public ValueTask DisposeAsync() => _scenario.DisposeAsync();

    [Fact]
    public async Task ANewKey_SignsIn_ButHoldsNoPermissions()
    {
        var created = await CreateKeyAsync("ci");

        var canReadUsers = await AsKeyAsync(
            created.Key!,
            services =>
                services
                    .GetRequiredService<IAuthorizationProvider>()
                    .IsAuthorizedAsync(AuthAction.Read, User.RESOURCE_TYPE)
        );

        Assert.Equal(CreateApiKeyResultCode.Success, created.ResultCode);
        Assert.StartsWith("ciam_", created.Key);
        Assert.False(canReadUsers);
    }

    [Fact]
    public async Task AKey_UsesTheRolesAssignedToIt()
    {
        var created = await CreateKeyAsync("reader");
        await AssignRolesAsync(created.CreatedApiKeyId, [_scenario.ReaderRoleId]);

        var result = await AsKeyAsync(
            created.Key!,
            async services =>
            {
                var context = services.GetRequiredService<IUserContextProvider>().GetUserContext()!;
                var users = await services
                    .GetRequiredService<IRetrievalService>()
                    .ListUsersAsync(new ListUsersRequest(_scenario.AccountId));
                return (context, users);
            }
        );

        Assert.Null(result.context.User);
        Assert.Equal(created.CreatedApiKeyId, result.context.ApiKey!.Id);
        Assert.Equal(_scenario.AccountId, result.context.CurrentAccount!.Id);
        Assert.Equal(RetrieveResultCode.Success, result.users.ResultCode);
    }

    [Fact]
    public async Task AKey_LosesPermissionsOnResourceTypesThePlatformHasNotSwitchedOn()
    {
        var created = await CreateKeyAsync("editor");
        await AssignRolesAsync(created.CreatedApiKeyId, [_scenario.EditorRoleId]);

        var canReadRole = await AsKeyAsync(
            created.Key!,
            services =>
                services
                    .GetRequiredService<IAuthorizationProvider>()
                    .IsAuthorizedAsync(AuthAction.Read, Role.RESOURCE_TYPE, _scenario.EditorRoleId)
        );

        Assert.False(canReadRole);
    }

    [Fact]
    public async Task AKey_UsesTheRolesOfItsGroups()
    {
        var created = await CreateKeyAsync("grouped");
        var added = await AddToGroupAsync(created.CreatedApiKeyId, _scenario.EditorGroupId);
        await AssignRolesAsync(created.CreatedApiKeyId, [_scenario.ReaderRoleId]);

        var canReadUsers = await AsKeyAsync(
            created.Key!,
            services =>
                services
                    .GetRequiredService<IAuthorizationProvider>()
                    .IsAuthorizedAsync(AuthAction.Read, User.RESOURCE_TYPE)
        );

        Assert.Equal(AddApiKeysToGroupResultCode.Success, added.ResultCode);
        Assert.True(canReadUsers);
    }

    [Fact]
    public async Task AWrongSecret_IsRefused_AndRepeatedOnesLockTheKey()
    {
        var created = await CreateKeyAsync("lockable");
        var wrong = created.Key![..^4] + "AAAA";

        for (var i = 0; i < 5; i++)
            Assert.Equal(
                SignInResultCode.InvalidApiKeyError,
                (await SignInAsync(wrong)).ResultCode
            );

        Assert.Equal(
            SignInResultCode.InvalidApiKeyError,
            (await SignInAsync(created.Key)).ResultCode
        );
    }

    [Fact]
    public async Task RotatingASlot_RetiresItsOldSecretAndSessions_AndLeavesTheOtherSlot()
    {
        var created = await CreateKeyAsync("rotating");
        var secondary = await RotateAsync(created.CreatedApiKeyId, ApiKeySlot.Secondary);
        var primarySession = await SignInAsync(created.Key!, "primary-client");
        var secondarySession = await SignInAsync(secondary.Key!, "secondary-client");

        var rotated = await RotateAsync(created.CreatedApiKeyId, ApiKeySlot.Primary);

        Assert.Equal(RotateApiKeySecretResultCode.Success, rotated.ResultCode);
        Assert.Equal(
            SignInResultCode.InvalidApiKeyError,
            (await SignInAsync(created.Key!)).ResultCode
        );
        Assert.Equal(SignInResultCode.Success, (await SignInAsync(rotated.Key!)).ResultCode);
        Assert.Equal(SignInResultCode.Success, (await SignInAsync(secondary.Key!)).ResultCode);
        Assert.NotEqual(
            RenewAuthTokenResultCode.Success,
            (await RenewAsync(primarySession.RefreshToken!)).ResultCode
        );
        Assert.Equal(
            RenewAuthTokenResultCode.Success,
            (await RenewAsync(secondarySession.RefreshToken!)).ResultCode
        );
    }

    [Fact]
    public async Task AKeySession_RenewsAndValidates_LikeAUserSession()
    {
        var created = await CreateKeyAsync("renewing");
        var signIn = await SignInAsync(created.Key!);
        _scenario.Host.TimeProvider.AdvanceSeconds(60);

        var renewed = await RenewAsync(signIn.RefreshToken!);
        var validated = await _scenario.Host.WithScopeAsync(async services =>
        {
            var code = await services
                .GetRequiredService<IAuthenticationService>()
                .AuthenticateWithTokenAsync(renewed.AuthToken!);
            return (code, services.GetRequiredService<IUserContextProvider>().GetUserContext());
        });

        Assert.Equal(RenewAuthTokenResultCode.Success, renewed.ResultCode);
        Assert.Equal(UserAuthTokenValidationResultCode.Success, validated.code);
        Assert.Equal(created.CreatedApiKeyId, validated.Item2!.ApiKey!.Id);
    }

    [Fact]
    public async Task AKey_CannotSwitchAccounts()
    {
        var created = await CreateKeyAsync("switcher");

        var switched = await AsKeyAsync(
            created.Key!,
            services =>
                services
                    .GetRequiredService<IAuthenticationService>()
                    .SwitchAccountAsync(new SwitchAccountRequest(_scenario.OtherAccountId))
        );

        Assert.NotEqual(SignInResultCode.Success, switched.ResultCode);
    }

    [Fact]
    public async Task DeletingAKey_EndsItsSignIns_AndLeavesItsRolesDeletable()
    {
        var created = await CreateKeyAsync("doomed");
        await AssignRolesAsync(created.CreatedApiKeyId, [_scenario.ReaderRoleId]);

        var deleted = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IDeregistrationService>()
                    .DeregisterApiKeyAsync(
                        new DeregisterApiKeyRequest(created.CreatedApiKeyId, _scenario.AccountId)
                    )
        );

        Assert.Equal(DeregisterApiKeyResultCode.Success, deleted.ResultCode);
        Assert.Equal(
            SignInResultCode.InvalidApiKeyError,
            (await SignInAsync(created.Key!)).ResultCode
        );
        Assert.Equal(
            0,
            await _scenario.Host.QueryAsync(db =>
                db.ApiKeySecrets.AsNoTracking()
                    .CountAsync(s => s.ApiKeyId == created.CreatedApiKeyId)
            )
        );
    }

    [Fact]
    public async Task DeletingARoleAndAGroupAKeyHolds_ClearsTheMemberships()
    {
        var created = await CreateKeyAsync("member");
        await AssignRolesAsync(created.CreatedApiKeyId, [_scenario.ReaderRoleId]);
        await AddToGroupAsync(created.CreatedApiKeyId, _scenario.EditorGroupId);

        var roleDeleted = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IDeregistrationService>()
                    .DeregisterRoleAsync(
                        new DeregisterRoleRequest(_scenario.ReaderRoleId, _scenario.AccountId)
                    )
        );
        var groupDeleted = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IDeregistrationService>()
                    .DeregisterGroupAsync(
                        new DeregisterGroupRequest(_scenario.EditorGroupId, _scenario.AccountId)
                    )
        );

        Assert.Equal(DeregisterRoleResultCode.Success, roleDeleted.ResultCode);
        Assert.Equal(DeregisterGroupResultCode.Success, groupDeleted.ResultCode);
    }

    [Fact]
    public async Task AKeyWithApiKeyPermissions_CannotGrantRolesItDoesNotHold()
    {
        var created = await CreateKeyAsync("manager");
        var ownerPermissions = await _scenario.Host.QueryAsync(db =>
            db.Set<Permissions.Entities.PermissionEntity>()
                .AsNoTracking()
                .Where(p =>
                    p.AccountId == _scenario.AccountId
                    && p.ResourceType == ApiKey.RESOURCE_TYPE
                    && p.IsSystemDefined
                )
                .Select(p => p.Id)
                .ToListAsync()
        );
        var managerRole = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            async services =>
            {
                var registration = services.GetRequiredService<IRegistrationService>();
                var role = await registration.RegisterRoleAsync(
                    new RegisterRoleRequest("Key managers", _scenario.AccountId)
                );
                await registration.RegisterPermissionsWithRoleAsync(
                    new RegisterPermissionsWithRoleRequest(
                        ownerPermissions,
                        role.CreatedRoleId,
                        _scenario.AccountId
                    )
                );
                return role.CreatedRoleId;
            }
        );
        await AssignRolesAsync(created.CreatedApiKeyId, [managerRole]);
        var second = await CreateKeyAsync("target");

        var grant = await AsKeyAsync(
            created.Key!,
            services =>
                services
                    .GetRequiredService<IRegistrationService>()
                    .RegisterRolesWithApiKeyAsync(
                        new RegisterRolesWithApiKeyRequest(
                            [_scenario.ReaderRoleId],
                            second.CreatedApiKeyId,
                            _scenario.AccountId
                        )
                    )
        );

        Assert.Equal(AssignRolesToApiKeyResultCode.UnauthorizedError, grant.ResultCode);

        await AssignRolesAsync(second.CreatedApiKeyId, [_scenario.ReaderRoleId]);
        var rotated = await AsKeyAsync(
            created.Key!,
            services =>
                services
                    .GetRequiredService<IModificationService>()
                    .RotateApiKeySecretAsync(
                        new RotateApiKeySecretRequest(
                            second.CreatedApiKeyId,
                            _scenario.AccountId,
                            ApiKeySlot.Primary
                        )
                    )
        );

        Assert.Equal(RotateApiKeySecretResultCode.UnauthorizedError, rotated.ResultCode);
        Assert.Null(rotated.Key);
        Assert.Equal(SignInResultCode.Success, (await SignInAsync(second.Key!)).ResultCode);
    }

    [Fact]
    public async Task AssigningRoles_SkipsOnesAlreadyHeldOrFromAnotherAccount()
    {
        var created = await CreateKeyAsync("partial");
        await AssignRolesAsync(created.CreatedApiKeyId, [_scenario.ReaderRoleId]);

        var second = await AssignRolesAsync(
            created.CreatedApiKeyId,
            [_scenario.ReaderRoleId, _scenario.EditorRoleId, Guid.CreateVersion7()]
        );

        Assert.Equal(AssignRolesToApiKeyResultCode.PartialSuccess, second.ResultCode);
        Assert.Equal(1, second.RegisteredRoleCount);
        Assert.Equal(2, second.InvalidRoleIds.Count);
    }

    [Fact]
    public async Task RemovingAKeyFromAGroup_EndsWhatTheGroupGave()
    {
        var created = await CreateKeyAsync("leaver");
        await AddToGroupAsync(created.CreatedApiKeyId, _scenario.EditorGroupId);

        var removed = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IDeregistrationService>()
                    .DeregisterApiKeysFromGroupAsync(
                        new DeregisterApiKeysFromGroupRequest(
                            [created.CreatedApiKeyId],
                            _scenario.EditorGroupId,
                            _scenario.AccountId
                        )
                    )
        );
        var key = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IRetrievalService>()
                    .GetApiKeyAsync(created.CreatedApiKeyId, hydrate: true)
        );

        Assert.Equal(DeregisterApiKeysFromGroupResultCode.Success, removed.ResultCode);
        Assert.Empty(key.Item!.Groups!);
    }

    [Fact]
    public async Task HydratedRolesAndGroups_ListTheKeysThatHoldThem()
    {
        var created = await CreateKeyAsync("holder");
        await AssignRolesAsync(created.CreatedApiKeyId, [_scenario.ReaderRoleId]);
        await AddToGroupAsync(created.CreatedApiKeyId, _scenario.EditorGroupId);

        var (role, group) = await _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            async services =>
            {
                var retrieval = services.GetRequiredService<IRetrievalService>();
                return (
                    await retrieval.GetRoleAsync(_scenario.ReaderRoleId, hydrate: true),
                    await retrieval.GetGroupAsync(_scenario.EditorGroupId, hydrate: true)
                );
            }
        );

        Assert.Equal(created.CreatedApiKeyId, Assert.Single(role.Item!.ApiKeys!).Id);
        Assert.Equal(created.CreatedApiKeyId, Assert.Single(group.Item!.ApiKeys!).Id);
    }

    private Task<RegisterApiKeyResult> CreateKeyAsync(string name) =>
        _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IRegistrationService>()
                    .RegisterApiKeyAsync(new RegisterApiKeyRequest(name, _scenario.AccountId))
        );

    private Task<RegisterRolesWithApiKeyResult> AssignRolesAsync(
        Guid apiKeyId,
        List<Guid> roleIds
    ) =>
        _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IRegistrationService>()
                    .RegisterRolesWithApiKeyAsync(
                        new RegisterRolesWithApiKeyRequest(roleIds, apiKeyId, _scenario.AccountId)
                    )
        );

    private Task<RegisterApiKeysWithGroupResult> AddToGroupAsync(Guid apiKeyId, Guid groupId) =>
        _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IRegistrationService>()
                    .RegisterApiKeysWithGroupAsync(
                        new RegisterApiKeysWithGroupRequest(
                            [apiKeyId],
                            groupId,
                            _scenario.AccountId
                        )
                    )
        );

    private Task<RotateApiKeySecretResult> RotateAsync(Guid apiKeyId, ApiKeySlot slot) =>
        _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IModificationService>()
                    .RotateApiKeySecretAsync(
                        new RotateApiKeySecretRequest(apiKeyId, _scenario.AccountId, slot)
                    )
        );

    private Task<SignInResult> SignInAsync(string apiKey, string deviceId = DEVICE) =>
        _scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IAuthenticationService>()
                .SignInWithApiKeyAsync(new SignInWithApiKeyRequest(apiKey, deviceId))
        );

    private Task<RenewAuthTokenResult> RenewAsync(string refreshToken) =>
        _scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IAuthenticationService>()
                .RenewAuthTokenAsync(new RenewAuthTokenRequest(refreshToken))
        );

    private Task<T> AsKeyAsync<T>(string apiKey, Func<IServiceProvider, Task<T>> work) =>
        _scenario.Host.WithScopeAsync(async services =>
        {
            var signIn = await services
                .GetRequiredService<IAuthenticationService>()
                .SignInWithApiKeyAsync(new SignInWithApiKeyRequest(apiKey, DEVICE));
            Assert.Equal(SignInResultCode.Success, signIn.ResultCode);
            return await work(services);
        });
}
